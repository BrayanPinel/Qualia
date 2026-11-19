using System.Net;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.IA;
using Qualia.Infrastructure.Procesamiento;

namespace Qualia.Tests.Unitarias;

/// Adaptadores reales (Deepgram + Groq) con respuestas HTTP falsas.
/// Verifica el contrato con los proveedores, el enmascarado previo a Groq y el guardado de JSON crudos.
public sealed class ProveedoresIATests : IDisposable
{
    private static readonly Guid Empresa = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Llamada = Guid.Parse("10000000-0000-0000-0005-000000000004");

    private readonly string _directorio = Path.Combine(Path.GetTempPath(), "qualia-pruebas-ia", Guid.NewGuid().ToString("N"));

    // Diarización fallida a propósito: todo llega como hablante 0.
    private const string RespuestaDeepgram = """
        {"metadata":{"duration":20.5},"results":{"utterances":[
          {"start":0.4,"end":3.2,"speaker":0,"transcript":"Buenas tardes, le saluda Alicia de Servicios Norte."},
          {"start":3.8,"end":5.1,"speaker":0,"transcript":"Hola, soy Gabriela."},
          {"start":5.3,"end":6.0,"speaker":0,"transcript":"Claro, es 0 8 0 1"},
          {"start":6.2,"end":7.9,"speaker":0,"transcript":"1 9 9 5 4 4 3 2 1."},
          {"start":8.5,"end":11.0,"speaker":0,"transcript":"Gracias, sus datos están verificados."},
          {"start":11.0,"end":11.0,"speaker":0,"transcript":""}
        ]}}
        """;

    private const string RespuestaGroqActores = """
        {"choices":[{"message":{"content":"{\"fragmentos\":[{\"n\":1,\"rol\":\"Agente\"},{\"n\":2,\"rol\":\"Cliente\"},{\"n\":3,\"rol\":\"cliente\"},{\"n\":4,\"rol\":\"Cliente\"},{\"n\":5,\"rol\":\"Agente\"}]}"}}],
         "usage":{"prompt_tokens":300,"completion_tokens":100}}
        """;

    [Fact]
    public async Task Deepgram_mas_Groq_reconoce_actores_aunque_la_diarizacion_falle_y_guarda_json_crudos()
    {
        var manejador = new ManejadorFalso()
            .Responder("api.deepgram.com", HttpStatusCode.OK, RespuestaDeepgram)
            .Responder("api.groq.com", HttpStatusCode.OK, RespuestaGroqActores);
        var transcriptor = CrearTranscriptor(manejador);

        var resultado = await transcriptor.TranscribirAsync(new SolicitudTranscripcion(Empresa, Llamada, "x.wav", 20.5m), CancellationToken.None);

        // Fragmentos consecutivos del mismo actor se unen en un turno.
        Assert.Equal(["Agente", "Cliente", "Agente"], resultado.Segmentos.Select(s => s.Hablante));
        var turnoCliente = resultado.Segmentos[1];
        Assert.Equal(3.8m, turnoCliente.InicioSeg);
        Assert.Equal(7.9m, turnoCliente.FinSeg);
        Assert.Equal("Hola, soy Gabriela. Claro, es 0 8 0 1 1 9 9 5 4 4 3 2 1.", turnoCliente.Texto);
        // Al unir el turno, el número dictado queda completo y el enmascarado lo detecta.
        Assert.Equal("Hola, soy Gabriela. Claro, es [NUMERO].", new EnmascaradorDatos().Enmascarar(turnoCliente.Texto));

        // Costo: 20.5 s de Deepgram + tokens de Groq.
        Assert.Equal(400, resultado.TokensConsumidos);
        Assert.True(resultado.CostoEstimado > 0);

        // RN-14: a Groq solo viaja texto enmascarado.
        var solicitudGroq = manejador.Cuerpos.Single(c => c.Host == "api.groq.com").Cuerpo;
        Assert.DoesNotContain("0 8 0 1", solicitudGroq);
        Assert.Contains("[NUMERO]", solicitudGroq);

        // JSON crudos guardados por empresa y llamada.
        var guardados = Directory.GetFiles(Path.Combine(_directorio, "RespuestasIA", Empresa.ToString(), Llamada.ToString()))
            .Select(Path.GetFileName).ToList();
        Assert.Contains(guardados, f => f!.EndsWith("_deepgram-transcripcion.json"));
        Assert.Contains(guardados, f => f!.EndsWith("_groq-actores-solicitud.json"));
        Assert.Contains(guardados, f => f!.EndsWith("_groq-actores-respuesta.json"));
        var crudo = File.ReadAllText(Directory.GetFiles(Path.Combine(_directorio, "RespuestasIA"), "*_deepgram-transcripcion.json", SearchOption.AllDirectories).Single());
        Assert.Equal(RespuestaDeepgram, crudo); // Exactamente lo que respondió el proveedor.
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, true)]
    [InlineData(HttpStatusCode.PaymentRequired, true)]
    [InlineData(HttpStatusCode.TooManyRequests, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, false)]
    public async Task Errores_de_Deepgram_se_clasifican_en_permanentes_o_reintentables(HttpStatusCode estado, bool permanente)
    {
        var transcriptor = CrearTranscriptor(new ManejadorFalso().Responder("api.deepgram.com", estado, "{}"));

        var error = await Assert.ThrowsAnyAsync<ErrorProcesamiento>(() =>
            transcriptor.TranscribirAsync(new SolicitudTranscripcion(Empresa, Llamada, "x.wav", 20.5m), CancellationToken.None));

        Assert.Equal(permanente, error is ErrorProcesamientoPermanente);
    }

    [Fact]
    public async Task Sin_grabacion_real_no_se_envia_audio_sintetico_a_Deepgram()
    {
        var manejador = new ManejadorFalso();
        var transcriptor = CrearTranscriptor(manejador, new AlmacenamientoFalso(sintetico: true));

        await Assert.ThrowsAsync<ErrorProcesamientoPermanente>(() =>
            transcriptor.TranscribirAsync(new SolicitudTranscripcion(Empresa, Llamada, "x.wav", 20.5m), CancellationToken.None));
        Assert.Empty(manejador.Cuerpos);
    }

    [Fact]
    public async Task Evaluador_Groq_traduce_ids_cortos_y_numeros_de_segmento()
    {
        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        const string contenido = """
            {"criterios":[
              {"id":"C1","puntaje":90,"justificacion":"Saluda.","evidencias":[{"segmento":1,"cita":"le saluda Alicia"}]},
              {"id":"c2","puntaje":"40","justificacion":"No verifica.","evidencias":[{"segmento":99,"cita":"inventado"}]}
            ]}
            """;
        var respuesta = new System.Text.Json.Nodes.JsonObject
        {
            ["choices"] = new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject
            {
                ["message"] = new System.Text.Json.Nodes.JsonObject { ["content"] = contenido },
            }),
            ["usage"] = new System.Text.Json.Nodes.JsonObject { ["prompt_tokens"] = 1000, ["completion_tokens"] = 200 },
        }.ToJsonString();
        var manejador = new ManejadorFalso().Responder("api.groq.com", HttpStatusCode.OK, respuesta);
        var evaluador = new EvaluadorGroq(CrearClienteGroq(manejador), Options.Create(new OpcionesGroq()));

        var resultado = await evaluador.EvaluarAsync(new SolicitudEvaluacion(Empresa, Llamada,
            [new(c1, "Saludo", "d", 50, false, 70), new(c2, "Identidad", "d", 50, true, 80)],
            [new(s1, 1, "Agente", "Buenas tardes, le saluda Alicia.")]), CancellationToken.None);

        Assert.Equal(ModoIA.Real, resultado.Modo);
        Assert.Equal("openai/gpt-oss-120b", resultado.ModeloUsado);
        Assert.Equal((1000, 200), (resultado.TokensEntrada, resultado.TokensSalida));
        Assert.Equal(c1, resultado.Criterios[0].CriterioId);
        Assert.Equal(s1, resultado.Criterios[0].Evidencias[0].SegmentoId);
        Assert.Equal(40m, resultado.Criterios[1].Puntaje);
        // Segmento inexistente: queda vacío para que ConstructorEvaluacion lo descarte (RN-09).
        Assert.Equal(Guid.Empty, resultado.Criterios[1].Evidencias[0].SegmentoId);

        // Los GUID no viajan al modelo; la transcripción sí, numerada.
        var solicitud = manejador.Cuerpos.Single().Cuerpo;
        Assert.DoesNotContain(c1.ToString(), solicitud);
        Assert.Contains("[1] Agente: Buenas tardes", System.Text.Json.JsonDocument.Parse(solicitud)
            .RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData("""{"fragmentos":[{"n":1,"rol":"Agente"}]}""")]                                        // falta el fragmento 2
    [InlineData("""{"fragmentos":[{"n":1,"rol":"Agente"},{"n":2,"rol":"Supervisor"}]}""")]           // rol desconocido
    [InlineData("""{"fragmentos":[{"n":1,"rol":"Agente"},{"n":1,"rol":"Cliente"}]}""")]              // repetido
    [InlineData("""no es json""")]
    public void Respuesta_de_actores_invalida_es_error_reintentable(string json)
    {
        Assert.Throws<ErrorProcesamiento>(() => IdentificadorActoresGroq.Interpretar(json, 2));
    }

    [Fact]
    public void Agrupador_une_turnos_del_mismo_actor_y_respeta_la_duracion()
    {
        var segmentos = AgrupadorEnunciados.Agrupar(
            [
                new(0, 0.5m, 2m, "Hola."), new(0, 2.3m, 4m, "¿En qué le ayudo?"),
                new(1, 4.5m, 6m, "Tengo un problema."),
                new(1, 9m, 12.5m, "Y otro más."), // pausa > 1.5 s: turno nuevo aunque sea el mismo actor
            ],
            ["Agente", "Agente", "Cliente", "Cliente"],
            duracionLlamada: 12m);

        Assert.Equal(3, segmentos.Count);
        Assert.Equal(new SegmentoTranscrito("Agente", 0.5m, 4m, "Hola. ¿En qué le ayudo?"), segmentos[0]);
        Assert.Equal(12m, segmentos[2].FinSeg); // ajustado a la duración registrada
    }

    public void Dispose()
    {
        if (Directory.Exists(_directorio))
        {
            Directory.Delete(_directorio, recursive: true);
        }
    }

    private TranscriptorDeepgram CrearTranscriptor(ManejadorFalso manejador, IAlmacenamientoAudio? audio = null)
    {
        var groq = CrearClienteGroq(manejador);
        return new TranscriptorDeepgram(
            new HttpClient(manejador) { BaseAddress = new Uri("https://api.deepgram.com/") },
            Options.Create(new OpcionesDeepgram()),
            audio ?? new AlmacenamientoFalso(sintetico: false),
            new EnmascaradorDatos(),
            new IdentificadorActoresGroq(groq, Options.Create(new OpcionesGroq())),
            CrearAlmacen(),
            NullLogger<TranscriptorDeepgram>.Instance);
    }

    private ClienteGroq CrearClienteGroq(ManejadorFalso manejador) => new(
        new HttpClient(manejador) { BaseAddress = new Uri("https://api.groq.com/openai/v1/") },
        Options.Create(new OpcionesGroq()),
        CrearAlmacen());

    private AlmacenRespuestasIA CrearAlmacen() => new(
        Options.Create(new OpcionesIA()), new EntornoFalso(_directorio), TimeProvider.System, NullLogger<AlmacenRespuestasIA>.Instance);

    [Fact]
    public async Task Groq_con_limite_de_peticiones_espera_y_reintenta()
    {
        var manejador = new ManejadorFalso()
            .Responder("api.groq.com", HttpStatusCode.TooManyRequests, """{"error":{"code":"rate_limit_exceeded"}}""")
            .Responder("api.groq.com", HttpStatusCode.OK, RespuestaGroqActores);
        var identificador = new IdentificadorActoresGroq(CrearClienteGroq(manejador), Options.Create(new OpcionesGroq()));

        var (roles, _) = await identificador.IdentificarAsync(Empresa, Llamada,
            [new(0, "a"), new(0, "b"), new(0, "c"), new(0, "d"), new(0, "e")], CancellationToken.None);

        Assert.Equal(5, roles.Count);
        Assert.Equal(2, manejador.Cuerpos.Count);
    }

    /// Responde en orden las respuestas registradas por host; la última se repite.
    private sealed class ManejadorFalso : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<(HttpStatusCode Estado, string Cuerpo)>> _respuestas = [];

        public List<(string Host, string Cuerpo)> Cuerpos { get; } = [];

        public ManejadorFalso Responder(string host, HttpStatusCode estado, string cuerpo)
        {
            if (!_respuestas.TryGetValue(host, out var cola))
            {
                _respuestas[host] = cola = new Queue<(HttpStatusCode, string)>();
            }

            cola.Enqueue((estado, cuerpo));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct)
        {
            var host = solicitud.RequestUri!.Host;
            var cuerpo = solicitud.Content is StringContent ? await solicitud.Content.ReadAsStringAsync(ct) : "(binario)";
            Cuerpos.Add((host, cuerpo));
            var cola = _respuestas[host];
            var (estado, respuesta) = cola.Count > 1 ? cola.Dequeue() : cola.Peek();
            var mensaje = new HttpResponseMessage(estado) { Content = new StringContent(respuesta, Encoding.UTF8, "application/json") };
            if (estado == HttpStatusCode.TooManyRequests)
            {
                mensaje.Headers.Add("retry-after", "0");
            }

            return mensaje;
        }
    }

    private sealed class AlmacenamientoFalso(bool sintetico) : IAlmacenamientoAudio
    {
        public Task<ContenidoAudio?> AbrirAsync(string archivoRuta, decimal duracionSeg, CancellationToken ct) =>
            Task.FromResult<ContenidoAudio?>(new ContenidoAudio(new MemoryStream([1, 2, 3]), "audio/wav", sintetico));
    }

    private sealed class EntornoFalso(string raiz) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Qualia.Tests";
        public string ContentRootPath { get; set; } = raiz;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
