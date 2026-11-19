using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// Rol B — revisión humana contra PostgreSQL real, sobre evaluaciones producidas por el motor del Rol D:
/// confirmación, ajuste completo con evidencias, conservación del resultado de IA y control de concurrencia.
[Collection(ColeccionPostgres.Nombre)]
public sealed class RevisionHumanaIntegracionTests : IAsyncLifetime
{
    private const string Motivo = "La IA subestimó la resolución: el agente explicó el ajuste completo.";
    private readonly QualiaApiFactory _api;

    public RevisionHumanaIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_revision_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    /// Recorrido del Rol D: la supervisora inicia la evaluación y el worker genera el borrador de IA.
    private async Task<EvaluacionDto> EvaluarConIAAsync(Guid llamadaId, Guid supervisorId)
    {
        var supervisor = _api.ClienteComo(supervisorId);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisor.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);
        await _api.ProcesarColaAsync();
        return await QualiaApiFactory.LeerAsync<EvaluacionDto>(await supervisor.GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));
    }

    private async Task<List<SegmentoDto>> SegmentosAsync(Guid llamadaId, Guid usuarioId) =>
        await QualiaApiFactory.LeerAsync<List<SegmentoDto>>(
            await _api.ClienteComo(usuarioId).GetAsync($"/api/llamadas/{llamadaId}/transcripcion"));

    /// Ajuste completo: cada criterio con puntaje fijo y, para el primero, una cita real del primer segmento.
    private static object AjusteCompleto(EvaluacionDto evaluacion, SegmentoDto segmento, int revisionEsperada, decimal puntaje = 55m) => new
    {
        revisionEsperada,
        motivo = Motivo,
        criterios = evaluacion.Criterios.Select((c, i) => new
        {
            criterioId = c.CriterioId,
            puntaje,
            justificacion = $"Revisión humana del criterio {c.Nombre}.",
            evidencias = i == 0
                ? new object[] { new { segmentoId = segmento.Id, citaTextual = segmento.TextoRedactado[..20] } }
                : [],
        }),
    };

    private static async Task<string> CodigoAsync(HttpResponseMessage r) => (await QualiaApiFactory.LeerAsync<ErrorDto>(r)).Codigo;

    [FactConDocker]
    public async Task Confirmar_oficializa_la_propuesta_y_una_revision_desactualizada_devuelve_409()
    {
        var llamadaId = Norte.Llamadas[0];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        var confirmada = await supervisora.PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 1 });
        Assert.Equal(HttpStatusCode.OK, confirmada.StatusCode);
        var evaluacion = await QualiaApiFactory.LeerAsync<EvaluacionDto>(confirmada);
        Assert.Equal(EstadoEvaluacion.ConfirmadaPorSupervisor, evaluacion.Estado);
        Assert.Equal(2, evaluacion.Revision);
        Assert.Equal(propuesta.PuntajeTotal, evaluacion.PuntajeTotal);
        Assert.All(evaluacion.Criterios, c => Assert.Null(c.PuntajeHumano));

        // Repetir con la revisión vieja: 409 REVISION_DESACTUALIZADA, sin sobrescribir.
        var repetida = await supervisora.PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 1 });
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Equal("REVISION_DESACTUALIZADA", await CodigoAsync(repetida));

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var revisiones = await db.RevisionesHumanas.IgnoreQueryFilters().Where(r => r.EvaluacionId == propuesta.Id).ToListAsync();
        var revision = Assert.Single(revisiones);
        Assert.Equal(Norte.Supervisora, revision.SupervisorId);
        Assert.False(await db.CriteriosRevisados.IgnoreQueryFilters().AnyAsync(c => c.RevisionId == revision.Id));
    }

    [FactConDocker]
    public async Task Ajuste_completo_conserva_el_resultado_de_IA_guarda_motivo_y_recalcula_el_total()
    {
        var llamadaId = Norte.Llamadas[1];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);
        var segmento = (await SegmentosAsync(llamadaId, Norte.Supervisora))[0];

        var respuesta = await _api.ClienteComo(Norte.Supervisora)
            .PutAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/ajuste", AjusteCompleto(propuesta, segmento, revisionEsperada: 1));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ajustada = await QualiaApiFactory.LeerAsync<EvaluacionDto>(respuesta);

        Assert.Equal(EstadoEvaluacion.AjustadaPorSupervisor, ajustada.Estado);
        Assert.Equal(2, ajustada.Revision);
        Assert.False(ajustada.RequiereRevision);
        Assert.Equal(55m, ajustada.PuntajeTotal); // Todos los criterios en 55: el total ponderado es 55.

        foreach (var criterio in ajustada.Criterios)
        {
            var original = propuesta.Criterios.Single(c => c.CriterioId == criterio.CriterioId);
            Assert.Equal(original.PuntajeIA, criterio.PuntajeIA);  // RN-12: la IA se conserva.
            Assert.Equal(55m, criterio.PuntajeHumano);
            Assert.Equal(55m, criterio.PuntajeVigente);
            Assert.StartsWith("Revisión humana del criterio", criterio.Justificacion);
        }

        var conCita = ajustada.Criterios.Single(c => c.CriterioId == propuesta.Criterios[0].CriterioId);
        var evidencia = Assert.Single(conCita.Evidencias);
        Assert.Equal(segmento.Id, evidencia.SegmentoId);
        Assert.Equal(segmento.InicioSeg, evidencia.InicioSeg); // RN-09: tiempos del segmento.
        Assert.All(ajustada.Criterios.Where(c => c.CriterioId != conCita.CriterioId), c => Assert.Empty(c.Evidencias));

        // En la base: Detalle_evaluacion intacto; Revision_humana + Criterio_revisado + Evidencia_revision.
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var detalles = await db.DetallesEvaluacion.IgnoreQueryFilters().Where(d => d.EvaluacionId == propuesta.Id).ToListAsync();
        Assert.All(detalles, d => Assert.Equal(propuesta.Criterios.Single(c => c.CriterioId == d.CriterioId).PuntajeIA, d.PuntajeCriterio));

        var revision = await db.RevisionesHumanas.IgnoreQueryFilters().Include(r => r.Criterios).ThenInclude(c => c.Evidencias)
            .SingleAsync(r => r.EvaluacionId == propuesta.Id);
        Assert.Equal(Motivo, revision.Comentarios);
        Assert.Equal(propuesta.Criterios.Count, revision.Criterios.Count);
        Assert.Single(revision.Criterios.SelectMany(c => c.Evidencias));

        // Una confirmación posterior no borra el ajuste: el puntaje vigente sigue siendo el humano.
        var confirmada = await QualiaApiFactory.LeerAsync<EvaluacionDto>(await _api.ClienteComo(Norte.Supervisora)
            .PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 2 }));
        Assert.Equal(3, confirmada.Revision);
        Assert.Equal(55m, confirmada.PuntajeTotal);
        Assert.All(confirmada.Criterios, c => Assert.Equal(55m, c.PuntajeHumano));

        // El agente propietario ve el resultado humano.
        var delAgente = await QualiaApiFactory.LeerAsync<EvaluacionDto>(
            await _api.ClienteComo(Norte.AgenteAlicia).GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));
        Assert.Equal(55m, delAgente.PuntajeTotal);
    }

    [FactConDocker]
    public async Task Ajuste_con_evidencia_invalida_o_incompleto_se_rechaza_sin_cambiar_la_evaluacion()
    {
        var llamadaId = Norte.Llamadas[2];
        var otraLlamada = Norte.Llamadas[4];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);
        await EvaluarConIAAsync(otraLlamada, Norte.Supervisora);
        var segmento = (await SegmentosAsync(llamadaId, Norte.Supervisora))[0];
        var segmentoAjeno = (await SegmentosAsync(otraLlamada, Norte.Supervisora))[0];
        var supervisora = _api.ClienteComo(Norte.Supervisora);
        var ruta = $"/api/evaluaciones/{propuesta.Id}/ajuste";

        object ConEvidencia(Guid segmentoId, string cita) => new
        {
            revisionEsperada = 1,
            motivo = Motivo,
            criterios = propuesta.Criterios.Select((c, i) => new
            {
                criterioId = c.CriterioId,
                puntaje = 80m,
                justificacion = "Justificación del supervisor.",
                evidencias = i == 0 ? new object[] { new { segmentoId, citaTextual = cita } } : [],
            }),
        };

        // Cita que no aparece en el segmento.
        var citaFalsa = await supervisora.PutAsJsonAsync(ruta, ConEvidencia(segmento.Id, "esta frase no fue dicha en la llamada"));
        Assert.Equal(HttpStatusCode.BadRequest, citaFalsa.StatusCode);
        var error = await QualiaApiFactory.LeerAsync<ErrorDto>(citaFalsa);
        Assert.Equal("EVIDENCIA_INVALIDA", error.Codigo);
        Assert.Contains("criterios[0].evidencias[0]", error.Errores!.Keys);

        // Segmento de OTRA llamada, aunque la cita sea literal.
        var ajeno = await supervisora.PutAsJsonAsync(ruta, ConEvidencia(segmentoAjeno.Id, segmentoAjeno.TextoRedactado[..15]));
        Assert.Equal(HttpStatusCode.BadRequest, ajeno.StatusCode);
        Assert.Equal("EVIDENCIA_INVALIDA", await CodigoAsync(ajeno));

        // Falta un criterio de la rúbrica.
        var incompleto = await supervisora.PutAsJsonAsync(ruta, new
        {
            revisionEsperada = 1,
            motivo = Motivo,
            criterios = propuesta.Criterios.Skip(1).Select(c => new
            {
                criterioId = c.CriterioId, puntaje = 70m, justificacion = "Justificación del supervisor.", evidencias = Array.Empty<object>(),
            }),
        });
        Assert.Equal(HttpStatusCode.BadRequest, incompleto.StatusCode);
        Assert.Equal("AJUSTE_INCOMPLETO", await CodigoAsync(incompleto));

        // Motivo demasiado corto: validación de forma.
        var sinMotivo = await supervisora.PutAsJsonAsync(ruta, new { revisionEsperada = 1, motivo = "corto", criterios = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
        Assert.Equal("VALIDACION", await CodigoAsync(sinMotivo));

        // Nada cambió.
        var actual = await QualiaApiFactory.LeerAsync<EvaluacionDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));
        Assert.Equal(1, actual.Revision);
        Assert.Equal(EstadoEvaluacion.EvaluadaPorIA, actual.Estado);

        // Con evidencia válida sí se acepta.
        Assert.Equal(HttpStatusCode.OK, (await supervisora.PutAsJsonAsync(ruta, AjusteCompleto(propuesta, segmento, 1))).StatusCode);
    }

    [FactConDocker]
    public async Task Evaluacion_marcada_con_evidencia_invalida_no_se_confirma_hasta_ajustarla()
    {
        var llamadaId = Norte.Llamadas[3];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);

        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
            await db.EvaluacionesIA.IgnoreQueryFilters().Where(e => e.Id == propuesta.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, true));
            var detalleId = await db.DetallesEvaluacion.IgnoreQueryFilters()
                .Where(d => d.EvaluacionId == propuesta.Id).Select(d => d.Id).FirstAsync();
            await db.DetallesEvaluacion.IgnoreQueryFilters().Where(d => d.Id == detalleId)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    d => d.Observacion, $"Se descartaron 1 {ConstructorEvaluacion.MarcaEvidenciaDescartada}; requiere revisión humana."));
        }

        var supervisora = _api.ClienteComo(Norte.Supervisora);
        var pendiente = await QualiaApiFactory.LeerAsync<EvaluacionDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));
        Assert.False(pendiente.Oficial);

        var confirmar = await supervisora.PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 1 });
        Assert.Equal(HttpStatusCode.Conflict, confirmar.StatusCode);
        Assert.Equal("EVIDENCIA_INVALIDA", await CodigoAsync(confirmar));

        // Una revisión que declara ausencia de evidencia con justificación lo corrige.
        var sinEvidencia = new
        {
            revisionEsperada = 1,
            motivo = Motivo,
            criterios = propuesta.Criterios.Select(c => new
            {
                criterioId = c.CriterioId,
                puntaje = c.PuntajeIA,
                justificacion = "No hay una cita literal que respalde este criterio.",
                evidencias = Array.Empty<object>(),
            }),
        };
        var ajustada = await QualiaApiFactory.LeerAsync<EvaluacionDto>(
            await supervisora.PutAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/ajuste", sinEvidencia));
        Assert.False(ajustada.RequiereRevision);
        Assert.All(ajustada.Criterios, c => Assert.Empty(c.Evidencias));

        Assert.Equal(HttpStatusCode.OK,
            (await supervisora.PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 2 })).StatusCode);
    }

    [FactConDocker]
    public async Task Nota_dudosa_de_la_IA_se_confirma_y_queda_oficial()
    {
        var llamadaId = Norte.Llamadas[4];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);

        // Falla sin evidencia descartada (falta sin fragmento o nota cerca del umbral).
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
            await db.EvaluacionesIA.IgnoreQueryFilters().Where(e => e.Id == propuesta.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, true));
            await db.DetallesEvaluacion.IgnoreQueryFilters().Where(d => d.EvaluacionId == propuesta.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Observacion, "La nota está cerca del umbral de cumplimiento; requiere revisión humana."));
        }

        var supervisora = _api.ClienteComo(Norte.Supervisora);
        var bandeja = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisora.GetAsync("/api/llamadas?requiereRevision=true"));
        Assert.Contains(bandeja.Items, l => l.Id == llamadaId && l.RequiereRevision == true);

        var confirmada = await QualiaApiFactory.LeerAsync<EvaluacionDto>(
            await supervisora.PostAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = 1 }));

        Assert.Equal(EstadoEvaluacion.ConfirmadaPorSupervisor, confirmada.Estado);
        Assert.False(confirmada.RequiereRevision);
        Assert.True(confirmada.Oficial);

        var despues = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisora.GetAsync("/api/llamadas?requiereRevision=true"));
        Assert.DoesNotContain(despues.Items, l => l.Id == llamadaId);
    }

    [FactConDocker]
    public async Task Dos_supervisores_con_la_misma_revision_esperada_solo_uno_gana()
    {
        var llamadaId = Norte.Llamadas[5];
        var propuesta = await EvaluarConIAAsync(llamadaId, Norte.Supervisora);
        var segmento = (await SegmentosAsync(llamadaId, Norte.Supervisora))[0];

        // Samuel también queda asignado a la campaña.
        Assert.Equal(HttpStatusCode.NoContent,
            (await _api.ClienteComo(Norte.Admin).PutAsync($"/api/campanas/{Norte.Campana}/usuarios/{Norte.SupervisorSinCampana}", null)).StatusCode);

        var respuestas = await Task.WhenAll(
            _api.ClienteComo(Norte.Supervisora).PutAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/ajuste", AjusteCompleto(propuesta, segmento, 1, 40m)),
            _api.ClienteComo(Norte.SupervisorSinCampana).PutAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/ajuste", AjusteCompleto(propuesta, segmento, 1, 90m)));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        var perdedora = Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("REVISION_DESACTUALIZADA", await CodigoAsync(perdedora));

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        Assert.Equal(1, await db.RevisionesHumanas.IgnoreQueryFilters().CountAsync(r => r.EvaluacionId == propuesta.Id));
        Assert.Equal(2, (await db.EvaluacionesIA.IgnoreQueryFilters().SingleAsync(e => e.Id == propuesta.Id)).Revision);
    }

    [FactConDocker]
    public async Task Solo_el_Supervisor_asignado_de_la_misma_empresa_revisa()
    {
        var propuesta = await EvaluarConIAAsync(Norte.Llamadas[0], Norte.Supervisora);
        var cuerpo = new { revisionEsperada = 1 };
        var ruta = $"/api/evaluaciones/{propuesta.Id}/confirmar";

        var noAsignado = await _api.ClienteComo(Norte.SupervisorSinCampana).PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.Forbidden, noAsignado.StatusCode);
        Assert.Equal("CAMPANA_NO_ASIGNADA", await CodigoAsync(noAsignado));

        var admin = await _api.ClienteComo(Norte.Admin).PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode); // AdminEmpresa no confirma por este rol.
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.AgenteAndres).PostAsJsonAsync(ruta, cuerpo)).StatusCode);

        var otraEmpresa = await _api.ClienteComo(Sur.Supervisor).PostAsJsonAsync(ruta, cuerpo);
        Assert.Equal(HttpStatusCode.NotFound, otraEmpresa.StatusCode);
        Assert.Equal("EVALUACION_NO_ENCONTRADA", await CodigoAsync(otraEmpresa));

        Assert.Equal(HttpStatusCode.NotFound,
            (await _api.ClienteComo(Norte.Supervisora).PostAsJsonAsync($"/api/evaluaciones/{Guid.NewGuid()}/confirmar", cuerpo)).StatusCode);
    }
}
