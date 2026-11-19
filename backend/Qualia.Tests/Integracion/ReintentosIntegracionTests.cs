using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Domain.Enums;
using Qualia.Domain.Suscripciones;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;

namespace Qualia.Tests.Integracion;

/// Reintentos limitados, motivo de fallo saneado y liberación del consumo (HU-13, RN-13).
[Collection(ColeccionPostgres.Nombre)]
public sealed class ReintentosIntegracionTests(PostgresFixture postgres)
{
    [FactConDocker]
    public async Task Error_recuperable_se_reintenta_y_termina_sin_duplicar_consumo()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_reintento_{Guid.NewGuid():N}", maximoIntentos: 3,
            s => { s.RemoveAll<ITranscriptor>(); s.AddSingleton<ITranscriptor>(new TranscriptorConFallos(1)); });

        var llamadaId = Norte.Llamadas[0];
        var supervisora = api.ClienteComo(Norte.Supervisora);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);

        // Primer ciclo: falla y queda pendiente de reintento, con motivo saneado visible.
        var ejecutor = api.Services.GetRequiredService<EjecutorProcesamiento>();
        await ejecutor.EjecutarCicloAsync(CancellationToken.None);

        var trasFallo = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.EnCola, trasFallo.Estado);
        Assert.Equal("Error interno durante el procesamiento.", trasFallo.MotivoFallo);
        Assert.DoesNotContain("Password", trasFallo.MotivoFallo);

        // Siguiente ciclo: se completa.
        await api.ProcesarColaAsync();

        var final = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.EvaluadaPorIA, final.Estado);
        Assert.Null(final.MotivoFallo);

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var trabajo = await db.TrabajosProcesamiento.IgnoreQueryFilters().SingleAsync(t => t.LlamadaId == llamadaId);
        Assert.Equal(EstadoTrabajo.Completado, trabajo.Estado);
        Assert.Equal(2, trabajo.Intentos);

        Assert.Equal(1, api.Consumo.Contar("Reservar", llamadaId));
        Assert.Equal(1, api.Consumo.Contar("Facturar", llamadaId));
        Assert.Equal(0, api.Consumo.Contar("Liberar", llamadaId));
    }

    [FactConDocker]
    public async Task Al_agotar_los_reintentos_la_llamada_falla_y_se_libera_la_reserva()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_fallo_{Guid.NewGuid():N}", maximoIntentos: 2,
            s => { s.RemoveAll<ITranscriptor>(); s.AddSingleton<ITranscriptor>(new TranscriptorConFallos(int.MaxValue)); });

        var llamadaId = Norte.Llamadas[1];
        var supervisora = api.ClienteComo(Norte.Supervisora);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);

        var procesados = await api.ProcesarColaAsync();
        Assert.Equal(2, procesados);

        var final = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.Fallida, final.Estado);
        Assert.False(string.IsNullOrWhiteSpace(final.MotivoFallo));

        Assert.Equal(HttpStatusCode.NotFound, (await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion")).StatusCode);
        Assert.Equal(1, api.Consumo.Contar("Liberar", llamadaId));
        Assert.Equal(0, api.Consumo.Contar("Facturar", llamadaId));

        // Un trabajo fallido no se vuelve a tomar.
        Assert.Equal(0, await api.ProcesarColaAsync());
    }

    [FactConDocker]
    public async Task Una_llamada_perdida_se_vuelve_a_evaluar_reutilizando_su_trabajo_y_su_consumo()
    {
        // Falla en los dos intentos permitidos y funciona al tercero (después del reintento manual).
        await using var api = new QualiaApiFactory(postgres, $"qualia_perdida_{Guid.NewGuid():N}", maximoIntentos: 2,
            s => { s.RemoveAll<ITranscriptor>(); s.AddSingleton<ITranscriptor>(new TranscriptorConFallos(2)); },
            usarConsumoReal: true);

        var llamadaId = Norte.Llamadas[2];
        var supervisora = api.ClienteComo(Norte.Supervisora);
        Assert.Equal(HttpStatusCode.Accepted, (await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);
        await api.ProcesarColaAsync();

        var perdida = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.Fallida, perdida.Estado);

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
            var liberado = await db.ConsumosLlamada.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.LlamadaId == llamadaId);
            Assert.Equal(EstadoConsumo.Liberado, liberado.Estado);
        }

        // Reintento manual: 202, la llamada vuelve a EnCola y se completa.
        var reintento = await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);
        Assert.Equal(HttpStatusCode.Accepted, reintento.StatusCode);
        Assert.Equal(1, await api.ProcesarColaAsync());

        var final = await QualiaApiFactory.LeerAsync<LlamadaDto>(await supervisora.GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.EvaluadaPorIA, final.Estado);
        Assert.Null(final.MotivoFallo);
        Assert.Equal(HttpStatusCode.OK, (await supervisora.GetAsync($"/api/llamadas/{llamadaId}/evaluacion")).StatusCode);

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
            var trabajo = await db.TrabajosProcesamiento.IgnoreQueryFilters().AsNoTracking().SingleAsync(t => t.LlamadaId == llamadaId);
            Assert.Equal(EstadoTrabajo.Completado, trabajo.Estado);
            Assert.Equal(1, trabajo.Intentos);

            // Un solo consumo por llamada: el liberado se reactivó y ahora está facturado.
            var consumo = await db.ConsumosLlamada.IgnoreQueryFilters().AsNoTracking().SingleAsync(c => c.LlamadaId == llamadaId);
            Assert.Equal(EstadoConsumo.Facturado, consumo.Estado);
            Assert.Equal(0m, consumo.MinutosReservados);
            Assert.True(consumo.MinutosFacturables > 0);
        }

        // Una llamada ya evaluada no admite otro reintento.
        Assert.Equal(HttpStatusCode.Conflict, (await supervisora.PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);
    }
}
