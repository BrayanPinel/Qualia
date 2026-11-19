using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Desempeno.Dtos;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// Scorecards y reporte de campaña (HU-09, HU-10, RN-11) contra PostgreSQL real.
[Collection(ColeccionPostgres.Nombre)]
public sealed class DesempenoIntegracionTests : IAsyncLifetime
{
    private const string Periodo = "2026-10";
    private static readonly DateTime FechaOctubre = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FechaSeptiembre = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly QualiaApiFactory _api;

    public DesempenoIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_desempeno_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task El_scorecard_cuenta_solo_evaluaciones_oficiales_sin_disputa_abierta()
    {
        await EvaluarTodoAsync(minimoMuestra: 2);
        var andres = _api.ClienteComo(Norte.AgenteAndres);

        var inicial = await ScorecardPropioAsync(andres);
        Assert.Equal(3, inicial.LlamadasEvaluadas);
        Assert.Equal(3, inicial.LlamadasConfirmadas);
        Assert.Equal(3, inicial.LlamadasElegibles);
        Assert.True(inicial.MuestraSuficiente);
        Assert.Equal(await PromedioEsperadoAsync(Norte.Llamadas[0], Norte.Llamadas[2], Norte.Llamadas[4]), inicial.PuntajePromedio);
        Assert.NotEmpty(inicial.Criterios);
        Assert.All(inicial.Criterios, c => Assert.Equal(3, c.Cantidad));
        Assert.Equal(Periodo, inicial.Periodo);

        // La IA marcó una falla: la evaluación deja de ser oficial.
        await MarcarParaRevisionAsync(Norte.Llamadas[0]);
        var conFalla = await ScorecardPropioAsync(andres);
        Assert.Equal(3, conFalla.LlamadasEvaluadas);
        Assert.Equal(2, conFalla.LlamadasConfirmadas);
        Assert.Equal(2, conFalla.LlamadasElegibles);

        // Una disputa abierta sigue contando como confirmada, pero no es elegible.
        var evaluacion = await EvaluacionAsync(Norte.Llamadas[2]);
        var disputa = await andres.PostAsJsonAsync($"/api/evaluaciones/{evaluacion.Id}/disputas",
            new { motivo = "No estoy de acuerdo con la nota del saludo." });
        Assert.Equal(HttpStatusCode.Created, disputa.StatusCode);

        var conDisputa = await ScorecardPropioAsync(andres);
        Assert.Equal(2, conDisputa.LlamadasConfirmadas);
        Assert.Equal(1, conDisputa.LlamadasElegibles);
        Assert.False(conDisputa.MuestraSuficiente);
        Assert.Null(conDisputa.PuntajePromedio);
        Assert.Empty(conDisputa.Criterios);
    }

    [FactConDocker]
    public async Task Con_el_minimo_predeterminado_la_muestra_es_insuficiente()
    {
        await EvaluarTodoAsync(minimoMuestra: 5);

        var scorecard = await ScorecardPropioAsync(_api.ClienteComo(Norte.AgenteAndres));

        Assert.Equal(3, scorecard.LlamadasElegibles);
        Assert.False(scorecard.MuestraSuficiente);
        Assert.Null(scorecard.PuntajePromedio);
        Assert.Null(scorecard.Tendencia);

        // La campaña sí alcanza el mínimo (6 llamadas elegibles entre los dos agentes).
        Assert.Equal(await PromedioEsperadoAsync(Norte.Llamadas), scorecard.PromedioCampana);
    }

    [FactConDocker]
    public async Task Tendencia_y_promedio_de_campana_se_calculan_con_muestra_en_ambos_meses()
    {
        await EvaluarTodoAsync(minimoMuestra: 1);
        await CambiarFechaAsync(Norte.Llamadas[4], FechaSeptiembre);

        var scorecard = await ScorecardPropioAsync(_api.ClienteComo(Norte.AgenteAndres));

        var octubre = await PromedioEsperadoAsync(Norte.Llamadas[0], Norte.Llamadas[2]);
        var septiembre = await PromedioEsperadoAsync(Norte.Llamadas[4]);
        Assert.Equal(2, scorecard.LlamadasElegibles);
        Assert.Equal(octubre, scorecard.PuntajePromedio);
        Assert.Equal(octubre - septiembre, scorecard.Tendencia);
        Assert.Equal(
            await PromedioEsperadoAsync(Norte.Llamadas[0], Norte.Llamadas[1], Norte.Llamadas[2], Norte.Llamadas[3], Norte.Llamadas[5]),
            scorecard.PromedioCampana);
    }

    [FactConDocker]
    public async Task Supervisor_ve_los_scorecards_de_su_campana_y_el_auditor_solo_el_reporte()
    {
        await EvaluarTodoAsync(minimoMuestra: 2);
        var supervisora = _api.ClienteComo(Norte.Supervisora);

        var equipo = await QualiaApiFactory.LeerAsync<PaginaDto<ScorecardDto>>(
            await supervisora.GetAsync($"/api/campanas/{Norte.Campana}/scorecards?periodo={Periodo}"));
        Assert.Equal(2, equipo.Total);
        Assert.Equal([Norte.AgenteAlicia, Norte.AgenteAndres], equipo.Items.Select(s => s.AgenteId));

        var deAndres = await QualiaApiFactory.LeerAsync<ScorecardDto>(
            await supervisora.GetAsync($"/api/agentes/{Norte.AgenteAndres}/scorecard?campanaId={Norte.Campana}&periodo={Periodo}"));
        Assert.Equal(3, deAndres.LlamadasElegibles);

        var reporte = await QualiaApiFactory.LeerAsync<ReporteCampanaDto>(
            await _api.ClienteComo(Norte.Auditor).GetAsync($"/api/reportes/campanas/{Norte.Campana}?periodo={Periodo}"));
        Assert.Equal(6, reporte.LlamadasElegibles);
        Assert.True(reporte.MuestraSuficiente);
        Assert.Equal(await PromedioEsperadoAsync(Norte.Llamadas), reporte.Promedio);

        // Permisos y aislamiento.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _api.ClienteComo(Norte.SupervisorSinCampana).GetAsync($"/api/campanas/{Norte.Campana}/scorecards")).StatusCode);
        Assert.Equal("ROL_NO_AUTORIZADO", await CodigoAsync(
            await _api.ClienteComo(Norte.Auditor).GetAsync($"/api/agentes/{Norte.AgenteAndres}/scorecard?campanaId={Norte.Campana}")));
        Assert.Equal("ROL_NO_AUTORIZADO", await CodigoAsync(
            await _api.ClienteComo(Norte.AgenteAndres).GetAsync($"/api/agentes/{Norte.AgenteAlicia}/scorecard?campanaId={Norte.Campana}")));
        Assert.Equal(HttpStatusCode.NotFound,
            (await _api.ClienteComo(Sur.Supervisor).GetAsync($"/api/reportes/campanas/{Norte.Campana}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await supervisora.GetAsync($"/api/agentes/{Sur.Agente}/scorecard?campanaId={Norte.Campana}")).StatusCode);
        Assert.Equal("VALIDACION", await CodigoAsync(
            await supervisora.GetAsync($"/api/reportes/campanas/{Norte.Campana}?periodo=2026-13")));
        Assert.Equal("VALIDACION", await CodigoAsync(
            await _api.ClienteComo(Norte.AgenteAndres).GetAsync("/api/agentes/me/scorecard")));
    }

    /// Evalúa todas las llamadas, las deja oficiales y en octubre, y fija el mínimo de muestra de Norte.
    private async Task EvaluarTodoAsync(int minimoMuestra)
    {
        var despachador = _api.Services.GetRequiredService<DespachadorEvaluacionAutomatica>();
        while (await despachador.EjecutarCicloAsync(CancellationToken.None) > 0)
        {
        }

        await _api.ProcesarColaAsync();

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        await db.EvaluacionesIA.IgnoreQueryFilters()
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, false));
        await db.Llamadas.IgnoreQueryFilters().Where(l => Norte.Llamadas.Contains(l.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.FechaLlamada, FechaOctubre));
        await db.Empresas.Where(e => e.Id == Norte.Empresa)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.MinimoMuestra, minimoMuestra));
    }

    private async Task MarcarParaRevisionAsync(Guid llamadaId)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        await db.EvaluacionesIA.IgnoreQueryFilters().Where(e => e.LlamadaId == llamadaId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.RequiereRevision, true));
    }

    private async Task CambiarFechaAsync(Guid llamadaId, DateTime fecha)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        await db.Llamadas.IgnoreQueryFilters().Where(l => l.Id == llamadaId)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.FechaLlamada, fecha));
    }

    private async Task<ScorecardDto> ScorecardPropioAsync(HttpClient agente) =>
        await QualiaApiFactory.LeerAsync<ScorecardDto>(
            await agente.GetAsync($"/api/agentes/me/scorecard?campanaId={Norte.Campana}&periodo={Periodo}"));

    private async Task<EvaluacionDto> EvaluacionAsync(Guid llamadaId) =>
        await QualiaApiFactory.LeerAsync<EvaluacionDto>(
            await _api.ClienteComo(Norte.Admin).GetAsync($"/api/llamadas/{llamadaId}/evaluacion"));

    /// Media de los totales por llamada, calculada desde la API de evaluaciones.
    private async Task<decimal> PromedioEsperadoAsync(params Guid[] llamadas)
    {
        var totales = new List<decimal>();
        foreach (var llamadaId in llamadas)
        {
            totales.Add((await EvaluacionAsync(llamadaId)).PuntajeTotal);
        }

        return Math.Round(totales.Average(), 2, MidpointRounding.AwayFromZero);
    }

    private static async Task<string?> CodigoAsync(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<ErrorDto>(QualiaApiFactory.Json))?.Codigo;
}
