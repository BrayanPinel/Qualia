using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Comun.Errores;
using Qualia.Application.Comun.Paginacion;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistencia;
using Qualia.Infrastructure.Persistencia.Semillas;
using Qualia.Infrastructure.Procesamiento;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistencia.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistencia.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// <summary>
/// Recorrido del Rol D contra PostgreSQL real: precarga → inicio de evaluación → worker →
/// transcripción enmascarada → evaluación simulada con evidencia → consulta, más permisos y aislamiento.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class LlamadasIntegracionTests : IAsyncLifetime
{
    private readonly QualiaApiFactory _api;

    public LlamadasIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_llamadas_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Flujo_completo_desde_llamada_precargada_hasta_borrador_de_IA_con_evidencia()
    {
        var llamadaId = Norte.Llamadas[0];
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        // 1. La supervisora ve la llamada disponible.
        var disponibles = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisora.GetAsync("/api/llamadas?estado=Disponible&tamanoPagina=100"));
        Assert.Contains(disponibles.Items, l => l.Id == llamadaId && l.CampanaId == Norte.Campana && l.VersionRubrica == 1);

        // 2. Inicia la evaluación: 202 + Location de seguimiento.
        var inicio = await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);
        Assert.Equal(HttpStatusCode.Accepted, inicio.StatusCode);
        Assert.Equal($"/api/llamadas/{llamadaId}", inicio.Headers.Location?.OriginalString);
        var aceptada = await QualiaApiFactory.LeerAsync<EvaluacionAceptadaDto>(inicio);
        Assert.Equal(EstadoLlamada.EnCola, aceptada.Estado);
        Assert.Equal(Norte.Empresa, aceptada.EmpresaId);
        Assert.Equal(1, _api.Consumo.Contar("Reservar", llamadaId));

        // Antes de procesar, la evaluación aún no existe.
        Assert.Equal(HttpStatusCode.NotFound, (await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion")).StatusCode);

        // 3. El worker procesa la cola.
        Assert.True(await _api.ProcesarColaAsync() >= 1);

        var detalle = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.EvaluadaPorIA, detalle.Estado);
        Assert.Null(detalle.MotivoFallo);

        // 4. Transcripción enmascarada (RN-14).
        var segmentos = await QualiaApiFactory.LeerAsync<List<SegmentoDto>>(
            await supervisora.GetAsync($"/api/llamadas/{llamadaId}/transcripcion"));
        Assert.NotEmpty(segmentos);
        Assert.Equal(Enumerable.Range(1, segmentos.Count), segmentos.Select(s => s.Orden));
        var textoCompleto = string.Join(" ", segmentos.Select(s => s.TextoRedactado));
        Assert.DoesNotMatch(@"\d{4}-\d{4}", textoCompleto);
        Assert.DoesNotContain("@", textoCompleto);

        // 5. Evaluación: un resultado por criterio, total calculado en servidor, evidencia con tiempos del segmento.
        var evaluacion = await QualiaApiFactory.LeerAsync<EvaluacionDto>(
            await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));
        Assert.Equal(EstadoEvaluacion.EvaluadaPorIA, evaluacion.Estado); // La IA no oficializa.
        Assert.Equal(ModoIA.Simulado, evaluacion.ModoIA);
        Assert.Equal(1, evaluacion.Revision);
        Assert.Equal(4, evaluacion.Criterios.Count);
        Assert.Equal(
            Math.Round(evaluacion.Criterios.Sum(c => c.PuntajeVigente * c.Peso / 100m), 2, MidpointRounding.AwayFromZero),
            evaluacion.PuntajeTotal);

        foreach (var criterio in evaluacion.Criterios)
        {
            Assert.Equal(criterio.PuntajeIA, criterio.PuntajeVigente);
            Assert.Null(criterio.PuntajeHumano);
            foreach (var evidencia in criterio.Evidencias)
            {
                var segmento = segmentos.Single(s => s.Id == evidencia.SegmentoId);
                Assert.Contains(evidencia.CitaTextual, segmento.TextoRedactado);
                Assert.Equal(segmento.InicioSeg, evidencia.InicioSeg);
                Assert.Equal(segmento.FinSeg, evidencia.FinSeg);
            }
        }

        // 6. Consumo facturado una sola vez; el agente propietario puede ver su evaluación.
        Assert.Equal(1, _api.Consumo.Contar("Facturar", llamadaId));
        var agente = _api.ClienteComo(Norte.AgenteAndres);
        Assert.Equal(HttpStatusCode.OK, (await agente.GetAsync($"/api/llamadas/{llamadaId}/evaluacion")).StatusCode);
    }

    [FactConDocker]
    public async Task Reprocesar_la_cola_no_duplica_trabajo_evaluacion_ni_facturacion()
    {
        var llamadaId = Norte.Llamadas[2];
        var supervisora = _api.ClienteComo(Norte.Supervisora);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);

        await _api.ProcesarColaAsync();
        await _api.ProcesarColaAsync();

        // Un segundo inicio sobre la misma llamada se rechaza.
        var repetido = await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal("EVALUACION_YA_INICIADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(repetido)).Codigo);

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        Assert.Equal(1, await db.TrabajosProcesamiento.IgnoreQueryFilters().CountAsync(t => t.LlamadaId == llamadaId));
        Assert.Equal(1, await db.EvaluacionesIA.IgnoreQueryFilters().CountAsync(e => e.LlamadaId == llamadaId));
        Assert.Equal(1, _api.Consumo.Contar("Reservar", llamadaId));
        Assert.Equal(1, _api.Consumo.Contar("Facturar", llamadaId));
    }

    [FactConDocker]
    public async Task Dos_supervisores_simultaneos_solo_uno_inicia_la_evaluacion()
    {
        var llamadaId = Norte.Llamadas[3];
        var clienteA = _api.ClienteComo(Norte.Supervisora);
        var clienteB = _api.ClienteComo(Norte.Supervisora);

        var respuestas = await Task.WhenAll(
            clienteA.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null),
            clienteB.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Accepted);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, _api.Consumo.Contar("Reservar", llamadaId));
    }

    [FactConDocker]
    public async Task Empresa_B_no_puede_ver_ni_evaluar_llamadas_de_empresa_A()
    {
        var supervisorSur = _api.ClienteComo(Sur.Supervisor);
        var llamadaNorte = Norte.Llamadas[1];

        foreach (var ruta in new[] { "", "/transcripcion", "/audio", "/evaluacion" })
        {
            var respuesta = await supervisorSur.GetAsync($"/api/llamadas/{llamadaNorte}{ruta}");
            Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await supervisorSur.PostAsync($"/api/llamadas/{llamadaNorte}/evaluacion", null)).StatusCode);

        var listado = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(await supervisorSur.GetAsync("/api/llamadas?tamanoPagina=100"));
        Assert.Equal(Sur.Llamadas.Length, listado.Total);
        Assert.All(listado.Items, l => Assert.Equal(Sur.Empresa, l.EmpresaId));

        // Ni filtrando explícitamente por la campaña de la otra empresa.
        var filtrado = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisorSur.GetAsync($"/api/llamadas?campanaId={Norte.Campana}"));
        Assert.Equal(0, filtrado.Total);
    }

    [FactConDocker]
    public async Task Cada_rol_ve_solo_las_llamadas_de_su_alcance()
    {
        var andres = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await _api.ClienteComo(Norte.AgenteAndres).GetAsync("/api/llamadas?tamanoPagina=100"));
        Assert.NotEmpty(andres.Items);
        Assert.All(andres.Items, l => Assert.Equal(Norte.AgenteAndres, l.AgenteId));

        var admin = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await _api.ClienteComo(Norte.Admin).GetAsync("/api/llamadas?tamanoPagina=100"));
        Assert.Equal(Norte.Llamadas.Length + 1, admin.Total);

        var supervisorSinCampana = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await _api.ClienteComo(Norte.SupervisorSinCampana).GetAsync("/api/llamadas"));
        Assert.Equal(0, supervisorSinCampana.Total);

        // Agente consultando la llamada de otra agente de su empresa → 403.
        var ajena = await _api.ClienteComo(Norte.AgenteAndres).GetAsync($"/api/llamadas/{Norte.Llamadas[1]}");
        Assert.Equal(HttpStatusCode.Forbidden, ajena.StatusCode);
        Assert.Equal("LLAMADA_FUERA_DE_ALCANCE", (await QualiaApiFactory.LeerAsync<ErrorDto>(ajena)).Codigo);

        // Auditor: sin acceso al contenido de llamadas.
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.Auditor).GetAsync("/api/llamadas")).StatusCode);
    }

    [FactConDocker]
    public async Task Solo_el_supervisor_asignado_puede_iniciar_la_evaluacion()
    {
        var llamadaId = Norte.Llamadas[4];

        var agente = await _api.ClienteComo(Norte.AgenteAndres).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);
        Assert.Equal(HttpStatusCode.Forbidden, agente.StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", (await QualiaApiFactory.LeerAsync<ErrorDto>(agente)).Codigo);

        var noAsignado = await _api.ClienteComo(Norte.SupervisorSinCampana).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);
        Assert.Equal(HttpStatusCode.Forbidden, noAsignado.StatusCode);
        Assert.Equal("CAMPANA_NO_ASIGNADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(noAsignado)).Codigo);

        Assert.Equal(0, _api.Consumo.Contar("Reservar", llamadaId));
    }

    [FactConDocker]
    public async Task Rubrica_en_borrador_no_permite_iniciar_la_evaluacion()
    {
        var respuesta = await _api.ClienteComo(Norte.Supervisora).PostAsync($"/api/llamadas/{Norte.LlamadaRubricaBorrador}/evaluacion", null);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("RUBRICA_SIN_PUBLICAR", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
    }

    [FactConDocker]
    public async Task Si_el_consumo_rechaza_la_reserva_no_queda_nada_a_medias()
    {
        var llamadaId = Norte.Llamadas[5];
        _api.Consumo.RechazarReserva = id => id == llamadaId
            ? ExcepcionNegocio.Prohibido("SUSCRIPCION_NO_ACTIVA", "La suscripción no permite iniciar evaluaciones.")
            : null;

        var respuesta = await _api.ClienteComo(Norte.Supervisora).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SUSCRIPCION_NO_ACTIVA", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);

        // La transacción se revirtió: la llamada sigue Disponible y no existe trabajo.
        var detalle = await QualiaApiFactory.LeerAsync<LlamadaDto>(await _api.ClienteComo(Norte.Admin).GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.Disponible, detalle.Estado);

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        Assert.False(await db.TrabajosProcesamiento.IgnoreQueryFilters().AnyAsync(t => t.LlamadaId == llamadaId));
    }

    [FactConDocker]
    public async Task Acceso_al_audio_entrega_url_temporal_firmada_reproducible_con_rangos()
    {
        var supervisora = _api.ClienteComo(Norte.Supervisora);
        var acceso = await QualiaApiFactory.LeerAsync<AccesoAudioDto>(await supervisora.GetAsync($"/api/llamadas/{Norte.Llamadas[0]}/audio"));

        Assert.True(acceso.ExpiraEn > DateTime.UtcNow);
        Assert.DoesNotContain("demo/norte", acceso.Url); // No expone la ruta interna.

        var ruta = new Uri(acceso.Url).PathAndQuery;
        var anonimo = _api.ClienteAnonimo();

        var audio = await anonimo.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, audio.StatusCode);
        Assert.Equal("audio/wav", audio.Content.Headers.ContentType?.MediaType);

        // Petición Range: permite posicionar el reproductor en el segmento de la evidencia.
        var parcial = new HttpRequestMessage(HttpMethod.Get, ruta);
        parcial.Headers.Range = new RangeHeaderValue(8000, 8999);
        var respuestaParcial = await anonimo.SendAsync(parcial);
        Assert.Equal(HttpStatusCode.PartialContent, respuestaParcial.StatusCode);
        Assert.Equal(1000, (await respuestaParcial.Content.ReadAsByteArrayAsync()).Length);

        // Token alterado → 404.
        var alterada = ruta[..^3] + (ruta[^3..] == "AAA" ? "BBB" : "AAA");
        Assert.Equal(HttpStatusCode.NotFound, (await anonimo.GetAsync(alterada)).StatusCode);
    }

    [FactConDocker]
    public async Task Peticiones_invalidas_o_sin_sesion_devuelven_errores_uniformes()
    {
        var sinSesion = await _api.ClienteAnonimo().GetAsync("/api/llamadas");
        Assert.Equal(HttpStatusCode.Unauthorized, sinSesion.StatusCode);

        var invalida = await _api.ClienteComo(Norte.Admin).GetAsync("/api/llamadas?tamanoPagina=500&estado=Inventado");
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(invalida);
        Assert.Equal("VALIDACION", error.Codigo);
        Assert.NotNull(error.Errores);
        Assert.Contains("TamanoPagina", error.Errores!.Keys);
        Assert.Contains("Estado", error.Errores.Keys);
        Assert.False(string.IsNullOrEmpty(error.TraceId));

        var inexistente = await _api.ClienteComo(Norte.Admin).GetAsync($"/api/llamadas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal("LLAMADA_NO_ENCONTRADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(inexistente)).Codigo);
    }

    [FactConDocker]
    public async Task El_worker_solo_escribe_datos_de_la_empresa_del_trabajo()
    {
        var llamadaSur = Sur.Llamadas[0];
        Assert.Equal(HttpStatusCode.Accepted,
            (await _api.ClienteComo(Sur.Supervisor).PostAsync($"/api/llamadas/{llamadaSur}/evaluacion", null)).StatusCode);

        await _api.ProcesarColaAsync();

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var segmentos = await db.SegmentosTranscripcion.IgnoreQueryFilters().Where(s => s.LlamadaId == llamadaSur).ToListAsync();
        var evaluacion = await db.EvaluacionesIA.IgnoreQueryFilters().Include(e => e.Detalles).ThenInclude(d => d.Evidencias)
            .SingleAsync(e => e.LlamadaId == llamadaSur);

        Assert.All(segmentos, s => Assert.Equal(Sur.Empresa, s.EmpresaId));
        Assert.Equal(Sur.Empresa, evaluacion.EmpresaId);
        Assert.All(evaluacion.Detalles, d =>
        {
            Assert.Equal(Sur.Empresa, d.EmpresaId);
            Assert.All(d.Evidencias, e => Assert.Contains(segmentos, s => s.Id == e.SegmentoId));
        });
        Assert.Equal(EvaluadorIASimulado.Modelo, evaluacion.ModeloUsado);
    }
}
