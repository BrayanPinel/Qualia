using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;

namespace Qualia.Tests.Integracion;

/// La IA califica sola las llamadas precargadas; el supervisor solo revisa las que la IA marca con falla
/// y ve el fragmento donde el agente cometió cada falta.
[Collection(ColeccionPostgres.Nombre)]
public sealed class EvaluacionAutomaticaIntegracionTests : IAsyncLifetime
{
    private readonly QualiaApiFactory _api;

    public EvaluacionAutomaticaIntegracionTests(PostgresFixture postgres)
    {
        _api = new QualiaApiFactory(postgres, $"qualia_automatica_{Guid.NewGuid():N}");
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [FactConDocker]
    public async Task Las_llamadas_con_rubrica_publicada_se_evaluan_sin_intervencion_del_supervisor()
    {
        var encoladas = await DespacharTodoAsync();
        await _api.ProcesarColaAsync();

        Assert.True(encoladas > 0);

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();

        // Solo quedan Disponible las llamadas cuya rúbrica sigue en borrador.
        var pendientes = await db.Llamadas.IgnoreQueryFilters()
            .Where(l => l.Estado == EstadoLlamada.Disponible)
            .Select(l => l.Rubrica!.Estado)
            .ToListAsync();
        Assert.All(pendientes, estado => Assert.Equal(EstadoRubrica.Borrador, estado));

        var evaluaciones = await db.EvaluacionesIA.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(encoladas, evaluaciones.Count);
        Assert.All(evaluaciones, e => Assert.Equal(EstadoEvaluacion.EvaluadaPorIA, e.Estado));
        Assert.Empty(await db.RevisionesHumanas.IgnoreQueryFilters().ToListAsync());

        // Un segundo ciclo no vuelve a encolar nada.
        Assert.Equal(0, await DespacharTodoAsync());
    }

    [FactConDocker]
    public async Task La_bandeja_muestra_solo_las_fallas_y_cada_falta_trae_su_fragmento()
    {
        await DespacharTodoAsync();
        await _api.ProcesarColaAsync();

        var supervisora = _api.ClienteComo(Norte.Supervisora);
        var todas = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisora.GetAsync("/api/llamadas?estado=EvaluadaPorIA&tamanoPagina=100"));
        var bandeja = await QualiaApiFactory.LeerAsync<PaginaDto<LlamadaDto>>(
            await supervisora.GetAsync("/api/llamadas?requiereRevision=true&tamanoPagina=100"));

        Assert.NotEmpty(todas.Items);
        Assert.All(bandeja.Items, l => Assert.True(l.RequiereRevision));
        Assert.Equal(todas.Items.Count(l => l.RequiereRevision == true), bandeja.Total);

        var conFragmento = 0;
        foreach (var llamada in todas.Items)
        {
            var evaluacion = await QualiaApiFactory.LeerAsync<EvaluacionDto>(
                await supervisora.GetAsync($"/api/llamadas/{llamada.Id}/evaluacion"));

            Assert.Equal(!evaluacion.RequiereRevision, evaluacion.Oficial);

            foreach (var criterio in evaluacion.Criterios)
            {
                Assert.Equal(criterio.PuntajeVigente >= criterio.UmbralCumplimiento, criterio.Cumple);

                // Falta sin fragmento: la IA no la sustenta y la evaluación va a revisión.
                if (!criterio.Cumple && criterio.Evidencias.Count == 0)
                {
                    Assert.True(evaluacion.RequiereRevision);
                }

                foreach (var evidencia in criterio.Evidencias)
                {
                    Assert.Contains(evidencia.CitaTextual, evidencia.TextoSegmento);
                    Assert.False(string.IsNullOrWhiteSpace(evidencia.Hablante));
                    Assert.True(evidencia.FinSeg >= evidencia.InicioSeg);
                    if (!criterio.Cumple)
                    {
                        conFragmento++;
                    }
                }
            }
        }

        // El simulador cita la intervención donde el agente falló en parte de las faltas.
        Assert.True(conFragmento > 0, "Ninguna falta trajo el fragmento donde ocurrió.");
    }

    [FactConDocker]
    public async Task El_inicio_manual_responde_409_si_la_llamada_ya_se_encolo_sola()
    {
        await DespacharTodoAsync();

        var respuesta = await _api.ClienteComo(Norte.Supervisora).PostAsync($"/api/llamadas/{Norte.Llamadas[0]}/evaluacion", null);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    private async Task<int> DespacharTodoAsync()
    {
        var despachador = _api.Services.GetRequiredService<DespachadorEvaluacionAutomatica>();
        var total = 0;
        int encoladas;
        do
        {
            encoladas = await despachador.EjecutarCicloAsync(CancellationToken.None);
            total += encoladas;
        }
        while (encoladas > 0 && total < 100);

        return total;
    }
}
