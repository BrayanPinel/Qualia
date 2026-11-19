using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Disputas;
using Qualia.Application.Features.Disputas.Dtos;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// Disputas contra PostgreSQL real con el recorrido completo: el worker evalúa con IA, la supervisora confirma,
/// el agente disputa y la supervisora mantiene o modifica las notas (contrato de revisión humana).
/// Norte tiene plan Pro (con disputas); Sur, Básico (sin disputas).
[Collection(ColeccionPostgres.Nombre)]
public sealed class DisputasIntegracionTests : IAsyncLifetime
{
    private const string Motivo = "No estoy de acuerdo: sí verifiqué la identidad del cliente al inicio.";
    private const string Resolucion = "Se revisó el audio completo y la evaluación se ajusta a la evidencia.";
    private readonly QualiaApiFactory _api;

    public DisputasIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_disputas_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task El_agente_disputa_su_evaluacion_oficial_y_cada_rol_ve_solo_lo_suyo()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);

        var creada = await DisputarAsync(Norte.AgenteAndres, evaluacion.Id);

        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var disputa = await QualiaApiFactory.LeerAsync<DisputaDto>(creada);
        Assert.Equal(EstadoDisputa.Abierta, disputa.Estado);
        Assert.Equal(evaluacion.Id, disputa.EvaluacionId);
        Assert.Null(disputa.Resolucion);
        Assert.Null(disputa.ResueltaPorId);

        Assert.Equal(1, (await ListarAsync(Norte.AgenteAndres)).Total);
        Assert.Equal(0, (await ListarAsync(Norte.AgenteAlicia)).Total);
        Assert.Equal(1, (await ListarAsync(Norte.Supervisora)).Total);
        Assert.Equal(0, (await ListarAsync(Norte.SupervisorSinCampana)).Total);
        Assert.Equal(1, (await ListarAsync(Norte.Admin)).Total);
        Assert.Equal(0, (await ListarAsync(Norte.Admin, "?estado=Resuelta")).Total);
        Assert.Equal(HttpStatusCode.Forbidden, (await _api.ClienteComo(Norte.Auditor).GetAsync("/api/disputas")).StatusCode);
    }

    [FactConDocker]
    public async Task Reglas_para_abrir_una_disputa()
    {
        var oficial = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);
        var pendiente = await EvaluarConIAAsync(Norte.Llamadas[2], Norte.Supervisora);
        await MarcarParaRevisionAsync(pendiente.Id);

        Assert.Equal(GestionDisputas.CodigoEvaluacionNoOficial, await CodigoAsync(await DisputarAsync(Norte.AgenteAndres, pendiente.Id)));
        Assert.Equal(GestionDisputas.CodigoEvaluacionFueraDeAlcance, await CodigoAsync(await DisputarAsync(Norte.AgenteAlicia, oficial.Id)));
        Assert.Equal(HttpStatusCode.NotFound, (await DisputarAsync(Norte.AgenteAndres, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await DisputarAsync(Norte.AgenteAndres, oficial.Id, "Muy corto")).StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", await CodigoAsync(await DisputarAsync(Norte.Supervisora, oficial.Id)));

        Assert.Equal(HttpStatusCode.Created, (await DisputarAsync(Norte.AgenteAndres, oficial.Id)).StatusCode);
        var repetida = await DisputarAsync(Norte.AgenteAndres, oficial.Id);
        Assert.Equal(HttpStatusCode.Conflict, repetida.StatusCode);
        Assert.Equal(GestionDisputas.CodigoDisputaAbierta, await CodigoAsync(repetida));
    }

    [FactConDocker]
    public async Task Un_plan_sin_disputas_las_rechaza()
    {
        var evaluacion = await EvaluacionOficialAsync(Sur.Llamadas[0], Sur.Supervisor);

        var respuesta = await DisputarAsync(Sur.Agente, evaluacion.Id);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(GestionDisputas.CodigoFuncionNoIncluida, await CodigoAsync(respuesta));
    }

    [FactConDocker]
    public async Task Mantener_resuelve_sin_cambiar_notas_e_incrementa_la_revision()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);
        var disputa = await QualiaApiFactory.LeerAsync<DisputaDto>(await DisputarAsync(Norte.AgenteAndres, evaluacion.Id));

        var respuesta = await ResolverAsync(Norte.Supervisora, disputa.Id, new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resuelta = await QualiaApiFactory.LeerAsync<DisputaDto>(respuesta);
        Assert.Equal(EstadoDisputa.Resuelta, resuelta.Estado);
        Assert.Equal(Resolucion, resuelta.Resolucion);
        Assert.Equal(Norte.Supervisora, resuelta.ResueltaPorId);
        Assert.NotNull(resuelta.ResueltaEn);

        var despues = await EvaluacionAsync(Norte.Llamadas[0], Norte.Supervisora);
        Assert.Equal(evaluacion.Revision + 1, despues.Revision);
        Assert.Equal(EstadoEvaluacion.ConfirmadaPorSupervisor, despues.Estado);
        Assert.Equal(evaluacion.PuntajeTotal, despues.PuntajeTotal);

        var otraVez = await ResolverAsync(Norte.Supervisora, disputa.Id, new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = despues.Revision });
        Assert.Equal(GestionDisputas.CodigoDisputaResuelta, await CodigoAsync(otraVez));

        // Resuelta la anterior, el agente puede volver a disputar.
        Assert.Equal(HttpStatusCode.Created, (await DisputarAsync(Norte.AgenteAndres, evaluacion.Id)).StatusCode);

        await using var alcance = _api.Services.CreateAsyncScope();
        var auditoria = await alcance.ServiceProvider.GetRequiredService<QualiaDbContext>().Auditorias.IgnoreQueryFilters()
            .SingleAsync(a => a.Accion == "ResolverDisputa");   // inspección sin sesión
        Assert.Equal(Norte.Supervisora, auditoria.UsuarioId);
    }

    [FactConDocker]
    public async Task Modificar_aplica_el_ajuste_completo_mediante_la_revision_humana()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);
        var disputa = await QualiaApiFactory.LeerAsync<DisputaDto>(await DisputarAsync(Norte.AgenteAndres, evaluacion.Id));
        var segmento = (await SegmentosAsync(Norte.Llamadas[0], Norte.Supervisora))[0];

        var respuesta = await ResolverAsync(Norte.Supervisora, disputa.Id, new
        {
            decision = "Modificar",
            resolucion = Resolucion,
            revisionEsperada = evaluacion.Revision,
            criterios = Ajustes(evaluacion, segmento, 77m),
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var despues = await EvaluacionAsync(Norte.Llamadas[0], Norte.Supervisora);
        Assert.Equal(EstadoEvaluacion.AjustadaPorSupervisor, despues.Estado);
        Assert.All(despues.Criterios, c => Assert.Equal(77m, c.PuntajeVigente));
        Assert.Equal(evaluacion.Criterios.Select(c => c.PuntajeIA), despues.Criterios.Select(c => c.PuntajeIA));
    }

    [FactConDocker]
    public async Task Un_error_en_la_resolucion_deja_la_disputa_abierta()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);
        var disputa = await QualiaApiFactory.LeerAsync<DisputaDto>(await DisputarAsync(Norte.AgenteAndres, evaluacion.Id));
        var segmento = (await SegmentosAsync(Norte.Llamadas[0], Norte.Supervisora))[0];

        var desactualizada = await ResolverAsync(Norte.Supervisora, disputa.Id, new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision + 5 });
        var incompleta = await ResolverAsync(Norte.Supervisora, disputa.Id, new
        {
            decision = "Modificar", resolucion = Resolucion, revisionEsperada = evaluacion.Revision,
            criterios = Ajustes(evaluacion, segmento, 60m).Take(1),
        });
        var mantenerConCriterios = await ResolverAsync(Norte.Supervisora, disputa.Id, new
        {
            decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision,
            criterios = Ajustes(evaluacion, segmento, 60m),
        });
        var noAsignado = await ResolverAsync(Norte.SupervisorSinCampana, disputa.Id, new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision });
        var otraEmpresa = await ResolverAsync(Sur.Supervisor, disputa.Id, new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision });

        Assert.Equal("REVISION_DESACTUALIZADA", await CodigoAsync(desactualizada));
        Assert.True(incompleta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.BadRequest, mantenerConCriterios.StatusCode);
        Assert.Equal("CAMPANA_NO_ASIGNADA", await CodigoAsync(noAsignado));
        Assert.Equal(HttpStatusCode.NotFound, otraEmpresa.StatusCode);

        var actual = (await ListarAsync(Norte.Admin)).Items.Single();
        Assert.Equal(EstadoDisputa.Abierta, actual.Estado);
        Assert.Equal(evaluacion.Revision, (await EvaluacionAsync(Norte.Llamadas[0], Norte.Supervisora)).Revision);
    }

    [FactConDocker]
    public async Task Dos_resoluciones_simultaneas_solo_aplican_una()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);
        var disputa = await QualiaApiFactory.LeerAsync<DisputaDto>(await DisputarAsync(Norte.AgenteAndres, evaluacion.Id));
        var cuerpo = new { decision = "Mantener", resolucion = Resolucion, revisionEsperada = evaluacion.Revision };

        var respuestas = await Task.WhenAll(
            ResolverAsync(Norte.Supervisora, disputa.Id, cuerpo),
            ResolverAsync(Norte.Supervisora, disputa.Id, cuerpo));

        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(evaluacion.Revision + 1, (await EvaluacionAsync(Norte.Llamadas[0], Norte.Supervisora)).Revision);
    }

    [FactConDocker]
    public async Task PostgreSQL_impone_una_disputa_abierta_por_evaluacion_y_resoluciones_completas()
    {
        var evaluacion = await EvaluacionOficialAsync(Norte.Llamadas[0], Norte.Supervisora);

        await AssertRechazoAsync("23505", db =>
        {
            db.Disputas.Add(NuevaDisputa(evaluacion.Id));
            db.Disputas.Add(NuevaDisputa(evaluacion.Id));
        });
        await AssertRechazoAsync("23514", db =>
        {
            var incompleta = NuevaDisputa(evaluacion.Id);
            incompleta.Estado = EstadoDisputa.Resuelta;   // sin resolución, resolutor ni fecha
            db.Disputas.Add(incompleta);
        });
    }

    private async Task AssertRechazoAsync(string sqlState, Action<QualiaDbContext> agregar)
    {
        await using var alcance = _api.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<QualiaDbContext>();
        agregar(db);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<Npgsql.PostgresException>(error.InnerException).SqlState);
    }

    private static Qualia.Domain.Entidades.Disputa NuevaDisputa(Guid evaluacionId) => new()
    {
        EmpresaId = Norte.Empresa, EvaluacionId = evaluacionId, Motivo = Motivo, CreadaEn = DateTime.UtcNow,
    };

    // ------------------------------------------------------------------ apoyo

    [FactConDocker]
    public async Task La_nota_oficial_de_la_IA_sin_fallas_se_puede_disputar_sin_revision_del_supervisor()
    {
        var evaluacion = await EvaluarConIAAsync(Norte.Llamadas[0], Norte.Supervisora);
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
            await db.EvaluacionesIA.IgnoreQueryFilters().Where(e => e.Id == evaluacion.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, false));
        }

        Assert.Equal(HttpStatusCode.Created, (await DisputarAsync(Norte.AgenteAndres, evaluacion.Id)).StatusCode);
    }

    private async Task MarcarParaRevisionAsync(Guid evaluacionId)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        await db.EvaluacionesIA.IgnoreQueryFilters().Where(e => e.Id == evaluacionId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, true));
    }

    private async Task<EvaluacionDto> EvaluarConIAAsync(Guid llamadaId, Guid supervisorId)
    {
        var supervisor = _api.ClienteComo(supervisorId);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisor.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);
        await _api.ProcesarColaAsync();
        return await EvaluacionAsync(llamadaId, supervisorId);
    }

    private async Task<EvaluacionDto> EvaluacionOficialAsync(Guid llamadaId, Guid supervisorId)
    {
        var propuesta = await EvaluarConIAAsync(llamadaId, supervisorId);
        var confirmada = await _api.ClienteComo(supervisorId).PostAsJsonAsync(
            $"/api/evaluaciones/{propuesta.Id}/confirmar", new { revisionEsperada = propuesta.Revision });

        if (confirmada.StatusCode != HttpStatusCode.OK)
        {
            // La IA simulada puede dejar evidencia inválida: se oficializa con un ajuste completo (mismo flujo de B).
            var segmento = (await SegmentosAsync(llamadaId, supervisorId))[0];
            var ajuste = await _api.ClienteComo(supervisorId).PutAsJsonAsync($"/api/evaluaciones/{propuesta.Id}/ajuste", new
            {
                revisionEsperada = propuesta.Revision,
                motivo = "Ajuste inicial del supervisor para oficializar la evaluación.",
                criterios = Ajustes(propuesta, segmento, 60m),
            });
            Assert.True(ajuste.IsSuccessStatusCode, await ajuste.Content.ReadAsStringAsync());
        }

        return await EvaluacionAsync(llamadaId, supervisorId);
    }

    private async Task<EvaluacionDto> EvaluacionAsync(Guid llamadaId, Guid usuarioId) =>
        await QualiaApiFactory.LeerAsync<EvaluacionDto>(await _api.ClienteComo(usuarioId).GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));

    private async Task<List<SegmentoDto>> SegmentosAsync(Guid llamadaId, Guid usuarioId) =>
        await QualiaApiFactory.LeerAsync<List<SegmentoDto>>(await _api.ClienteComo(usuarioId).GetAsync($"/api/llamadas/{llamadaId}/transcripcion"));

    /// Ajuste completo: todos los criterios con el mismo puntaje; el primero con una cita real.
    private static IEnumerable<object> Ajustes(EvaluacionDto evaluacion, SegmentoDto segmento, decimal puntaje) =>
        evaluacion.Criterios.Select((c, i) => (object)new
        {
            criterioId = c.CriterioId,
            puntaje,
            justificacion = $"Revisión del criterio {c.Nombre} tras la disputa.",
            evidencias = i == 0
                ? new object[] { new { segmentoId = segmento.Id, citaTextual = segmento.TextoRedactado[..20] } }
                : [],
        }).ToList();

    private Task<HttpResponseMessage> DisputarAsync(Guid agenteId, Guid evaluacionId, string motivo = Motivo) =>
        _api.ClienteComo(agenteId).PostAsJsonAsync($"/api/evaluaciones/{evaluacionId}/disputas", new { motivo });

    private Task<HttpResponseMessage> ResolverAsync(Guid supervisorId, Guid disputaId, object cuerpo) =>
        _api.ClienteComo(supervisorId).PutAsJsonAsync($"/api/disputas/{disputaId}/resolucion", cuerpo);

    private async Task<PaginaDto<DisputaDto>> ListarAsync(Guid usuarioId, string consulta = "") =>
        await QualiaApiFactory.LeerAsync<PaginaDto<DisputaDto>>(await _api.ClienteComo(usuarioId).GetAsync($"/api/disputas{consulta}"));

    private static async Task<string> CodigoAsync(HttpResponseMessage respuesta) =>
        (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo;
}
