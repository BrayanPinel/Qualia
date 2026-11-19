using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Audio;
using Qualia.Infrastructure.IA;
using Qualia.Infrastructure.Procesamiento;
using Xunit.Abstractions;

namespace Qualia.Tests.Integracion;

/// Mide el reconocimiento de actores con los proveedores reales contra el ground data de cada audio demo.
/// Consume crédito de Deepgram, por eso solo corre con la variable QUALIA_PRUEBAS_IA_REAL=1.
/// Escribe el resultado en Qualia.API/AudiosDemo/reporte-reconocimiento-actores.json.
public sealed class ReconocimientoActoresRealTests(ITestOutputHelper salida)
{
    private const double PasoSeg = 0.1;
    private const double ExactitudMinima = 0.90;
    private static readonly Guid EmpresaPruebas = Guid.Parse("99999999-0000-0000-0000-000000000001");

    [FactConIAReal]
    public async Task Deepgram_y_Groq_reconocen_al_agente_y_al_cliente_segun_el_ground_data()
    {
        var raizApi = BuscarCarpetaApi();
        var entorno = new Entorno(raizApi);
        var opcionesAudio = Options.Create(new OpcionesAudio { GenerarAudioSinteticoSiFalta = false });
        var lector = new LectorGroundData(opcionesAudio, entorno);
        var almacen = new AlmacenRespuestasIA(Options.Create(new OpcionesIA()), entorno, TimeProvider.System, NullLogger<AlmacenRespuestasIA>.Instance);

        var deepgram = new OpcionesDeepgram { ApiKey = Llave(raizApi, "Deepgram__ApiKey") };
        var groq = new OpcionesGroq { ApiKey = Llave(raizApi, "Groq__ApiKey") };
        var clienteGroq = new ClienteGroq(
            new HttpClient { BaseAddress = new Uri(groq.UrlBase), Timeout = TimeSpan.FromSeconds(groq.TiempoEsperaSeg), DefaultRequestHeaders = { { "Authorization", $"Bearer {groq.ApiKey}" } } },
            Options.Create(groq), almacen);
        var transcriptor = new TranscriptorDeepgram(
            new HttpClient { BaseAddress = new Uri(deepgram.UrlBase), Timeout = TimeSpan.FromSeconds(deepgram.TiempoEsperaSeg), DefaultRequestHeaders = { { "Authorization", $"Token {deepgram.ApiKey}" } } },
            Options.Create(deepgram),
            new AlmacenamientoAudioLocal(opcionesAudio, entorno),
            new EnmascaradorDatos(),
            new IdentificadorActoresGroq(clienteGroq, Options.Create(groq)),
            almacen,
            NullLogger<TranscriptorDeepgram>.Instance);

        var grounds = lector.LeerTodos();
        Assert.NotEmpty(grounds);
        var filas = new List<object>();
        double sumaLlm = 0, sumaDiarizacion = 0;

        foreach (var ground in grounds)
        {
            var llamadaId = IdDeterminista(ground.Archivo);
            var resultado = await transcriptor.TranscribirAsync(
                new SolicitudTranscripcion(EmpresaPruebas, llamadaId, ground.Archivo, ground.DuracionSeg), CancellationToken.None);

            // Línea base: solo diarización de Deepgram, el primer hablante es el agente.
            var crudo = Directory.GetFiles(Path.Combine(almacen.DirectorioBase, EmpresaPruebas.ToString(), llamadaId.ToString()), "*_deepgram-transcripcion.json")
                .Order().Last();
            var (enunciados, _) = TranscriptorDeepgram.Interpretar(File.ReadAllText(crudo));
            var primerHablante = enunciados[0].Hablante;
            var rolesDiarizacion = enunciados.Select(e => e.Hablante == primerHablante ? "Agente" : "Cliente").ToList();
            var segmentosDiarizacion = AgrupadorEnunciados.Agrupar(enunciados, rolesDiarizacion, ground.DuracionSeg);

            var (exactitudLlm, cobertura) = Medir(ground, resultado.Segmentos);
            var (exactitudDiarizacion, _) = Medir(ground, segmentosDiarizacion);
            sumaLlm += exactitudLlm;
            sumaDiarizacion += exactitudDiarizacion;

            var hablantes = enunciados.Select(e => e.Hablante).Distinct().Count();
            salida.WriteLine($"{ground.Archivo,-28} hablantes={hablantes}  diarización={exactitudDiarizacion:P1}  Deepgram+Groq={exactitudLlm:P1}  cobertura={cobertura:P0}");
            filas.Add(new
            {
                archivo = ground.Archivo,
                descripcion = ground.Descripcion,
                hablantesDetectadosPorDiarizacion = hablantes,
                exactitudSoloDiarizacion = Math.Round(exactitudDiarizacion, 4),
                exactitudDeepgramMasGroq = Math.Round(exactitudLlm, 4),
                coberturaDeVoz = Math.Round(cobertura, 4),
                segmentosReconocidos = resultado.Segmentos.Select(s => new { s.Hablante, s.InicioSeg, s.FinSeg }),
            });
        }

        var promedioLlm = sumaLlm / grounds.Count;
        var promedioDiarizacion = sumaDiarizacion / grounds.Count;
        salida.WriteLine($"PROMEDIO  diarización={promedioDiarizacion:P1}  Deepgram+Groq={promedioLlm:P1}");

        var reporte = new
        {
            generado = DateTime.UtcNow,
            metodo = "Por cada 0.1 s con voz reconocida se compara el actor reconocido con el actor del ground data.",
            transcriptor = $"Deepgram {deepgram.Modelo} ({deepgram.Idioma}, diarize=true)",
            reconocimientoActores = $"Groq {groq.ModeloActores}",
            promedioExactitudSoloDiarizacion = Math.Round(promedioDiarizacion, 4),
            promedioExactitudDeepgramMasGroq = Math.Round(promedioLlm, 4),
            llamadas = filas,
        };
        await File.WriteAllTextAsync(
            Path.Combine(raizApi, "AudiosDemo", "reporte-reconocimiento-actores.json"),
            JsonSerializer.Serialize(reporte, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

        Assert.True(promedioLlm >= ExactitudMinima, $"Exactitud promedio {promedioLlm:P1} menor a {ExactitudMinima:P0}");
    }

    /// Exactitud = muestras donde el actor reconocido coincide con el ground data / muestras con voz reconocida.
    /// Cobertura = muestras del ground data cubiertas por algún segmento reconocido.
    private static (double Exactitud, double Cobertura) Medir(GroundData ground, IReadOnlyList<SegmentoTranscrito> reconocidos)
    {
        int total = 0, cubiertas = 0, correctas = 0;
        foreach (var real in ground.Segmentos)
        {
            for (var t = (double)real.InicioSeg; t <= (double)real.FinSeg; t += PasoSeg)
            {
                total++;
                var segmento = reconocidos.FirstOrDefault(s => (double)s.InicioSeg <= t && t <= (double)s.FinSeg);
                if (segmento is null)
                {
                    continue;
                }

                cubiertas++;
                if (segmento.Hablante == real.Actor)
                {
                    correctas++;
                }
            }
        }

        return (cubiertas == 0 ? 0 : (double)correctas / cubiertas, total == 0 ? 0 : (double)cubiertas / total);
    }

    private static Guid IdDeterminista(string texto) => new(MD5.HashData(Encoding.UTF8.GetBytes(texto)));

    private static string BuscarCarpetaApi()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidato = Path.Combine(dir.FullName, "Qualia.API");
            if (Directory.Exists(Path.Combine(candidato, "AudiosDemo")))
            {
                return candidato;
            }
        }

        throw new DirectoryNotFoundException("No se encontró Qualia.API/AudiosDemo.");
    }

    internal static string? Llave(string raizApi, string clave)
    {
        var deEntorno = Environment.GetEnvironmentVariable(clave);
        if (!string.IsNullOrWhiteSpace(deEntorno))
        {
            return deEntorno;
        }

        var env = Path.Combine(raizApi, ".env");
        return File.Exists(env)
            ? File.ReadLines(env).Select(l => l.Trim()).Where(l => l.StartsWith(clave + "=")).Select(l => l[(clave.Length + 1)..].Trim('"')).LastOrDefault()
            : null;
    }

    private sealed class Entorno(string raiz) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Qualia.API";
        public string ContentRootPath { get; set; } = raiz;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

/// Solo corre con QUALIA_PRUEBAS_IA_REAL=1 (usa crédito de Deepgram y Groq).
public sealed class FactConIARealAttribute : FactAttribute
{
    public FactConIARealAttribute()
    {
        if (Environment.GetEnvironmentVariable("QUALIA_PRUEBAS_IA_REAL") != "1")
        {
            Skip = "Prueba con proveedores reales: defina QUALIA_PRUEBAS_IA_REAL=1 para ejecutarla.";
        }
    }
}
