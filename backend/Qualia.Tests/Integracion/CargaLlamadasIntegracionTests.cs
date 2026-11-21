using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;
using Qualia.Tests.Integracion.Infraestructura;
using Qualia.Tests.Unitarias;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// Carga de grabaciones propias (por ejemplo una nota de voz de WhatsApp) hasta su evaluación.
[Collection(ColeccionPostgres.Nombre)]
public sealed class CargaLlamadasIntegracionTests : IAsyncLifetime
{
    private readonly QualiaApiFactory _api;
    private readonly PostgresFixture _postgres;
    private readonly List<Guid> _cargadas = [];

    public CargaLlamadasIntegracionTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _api = new QualiaApiFactory(postgres, $"qualia_carga_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Los audios caen en la carpeta real de la API: se borran para no ensuciar el repositorio.
        var raiz = Path.Combine(_api.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "AudiosDemo", "cargas");
        foreach (var id in _cargadas)
        {
            foreach (var archivo in Directory.Exists(raiz) ? Directory.GetFiles(raiz, $"{id}.*", SearchOption.AllDirectories) : [])
            {
                File.Delete(archivo);
            }
        }

        await _api.DisposeAsync();
    }

    [FactConDocker]
    public async Task Nota_de_voz_cargada_se_mide_y_se_evalua_como_cualquier_llamada()
    {
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        var respuesta = await supervisora.PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.OggOpus(segundos: 42), "PTT-20261120-WA0001.opus", Norte.Campana, Norte.AgenteAndres));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var llamada = await QualiaApiFactory.LeerAsync<LlamadaDto>(respuesta);
        _cargadas.Add(llamada.Id);
        Assert.Equal($"/api/empresas/{Norte.Empresa}/llamadas/{llamada.Id}", respuesta.Headers.Location?.OriginalString);
        Assert.Equal(42m, llamada.DuracionSeg);
        Assert.Equal(EstadoLlamada.Disponible, llamada.Estado);
        Assert.Equal(Norte.Campana, llamada.CampanaId);
        Assert.Equal(Norte.Rubrica, llamada.RubricaId);

        // Se evalúa por el mismo camino que las precargadas.
        Assert.Equal(HttpStatusCode.Accepted, (await supervisora.PostAsync($"/api/llamadas/{llamada.Id}/evaluacion", null)).StatusCode);
        await _api.ProcesarColaAsync();

        var detalle = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamada.Id}"));
        Assert.Equal(EstadoLlamada.EvaluadaPorIA, detalle.Estado);
        Assert.Equal(HttpStatusCode.OK, (await supervisora.GetAsync($"/api/llamadas/{llamada.Id}/evaluacion")).StatusCode);

        // El audio subido se puede reproducir con la URL firmada.
        var acceso = await QualiaApiFactory.LeerAsync<AccesoAudioDto>(await supervisora.GetAsync($"/api/llamadas/{llamada.Id}/audio"));
        var audio = await _api.ClienteAnonimo().GetAsync(new Uri(acceso.Url).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, audio.StatusCode);
        Assert.Equal("audio/ogg", audio.Content.Headers.ContentType?.MediaType);
    }

    [FactConDocker]
    public async Task Campana_de_otra_empresa_responde_404()
    {
        var respuesta = await _api.ClienteComo(Sur.Supervisor).PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.Wav(segundos: 2), "audio.wav", Norte.Campana, Norte.AgenteAndres));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("CAMPANA_NO_ENCONTRADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
    }

    [FactConDocker]
    public async Task Agente_y_supervisor_sin_campana_no_pueden_cargar()
    {
        var agente = await _api.ClienteComo(Norte.AgenteAndres).PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.Wav(segundos: 2), "audio.wav", Norte.Campana, Norte.AgenteAndres));
        Assert.Equal(HttpStatusCode.Forbidden, agente.StatusCode);

        var sinCampana = await _api.ClienteComo(Norte.SupervisorSinCampana).PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.Wav(segundos: 2), "audio.wav", Norte.Campana, Norte.AgenteAndres));
        Assert.Equal(HttpStatusCode.Forbidden, sinCampana.StatusCode);
        Assert.Equal("CAMPANA_NO_ASIGNADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(sinCampana)).Codigo);
    }

    [FactConDocker]
    public async Task Archivo_invalido_o_agente_ajeno_responde_400()
    {
        var admin = _api.ClienteComo(Norte.Admin);

        var formato = await admin.PostAsync("/api/llamadas",
            Formulario("hola"u8.ToArray(), "notas.txt", Norte.Campana, Norte.AgenteAndres));
        Assert.Equal(HttpStatusCode.BadRequest, formato.StatusCode);
        Assert.Contains("audio", (await QualiaApiFactory.LeerAsync<ErrorDto>(formato)).Errores!.Keys);

        var danado = await admin.PostAsync("/api/llamadas",
            Formulario("no es audio"u8.ToArray(), "audio.ogg", Norte.Campana, Norte.AgenteAndres));
        Assert.Equal(HttpStatusCode.BadRequest, danado.StatusCode);

        var agenteSur = await admin.PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.Wav(segundos: 2), "audio.wav", Norte.Campana, Sur.Agente));
        Assert.Equal(HttpStatusCode.BadRequest, agenteSur.StatusCode);
        Assert.Contains("agenteId", (await QualiaApiFactory.LeerAsync<ErrorDto>(agenteSur)).Errores!.Keys);
    }

    [FactConDocker]
    public async Task Sin_la_opcion_habilitada_responde_403()
    {
        await using var api = new QualiaApiFactory(_postgres, $"qualia_carga_off_{Guid.NewGuid():N}",
            configurarServicios: s => s.Configure<OpcionesProcesamiento>(o => o.CargaManualHabilitada = false));

        var respuesta = await api.ClienteComo(Norte.Supervisora).PostAsync("/api/llamadas",
            Formulario(ArchivosAudio.Wav(segundos: 2), "audio.wav", Norte.Campana, Norte.AgenteAndres));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("CARGA_MANUAL_DESHABILITADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
    }

    private static MultipartFormDataContent Formulario(byte[] audio, string nombre, Guid campana, Guid agente)
    {
        var archivo = new ByteArrayContent(audio);
        archivo.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent
        {
            { archivo, "audio", nombre },
            { new StringContent(campana.ToString()), "campanaId" },
            { new StringContent(agente.ToString()), "agenteId" },
        };
    }
}
