using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Enums;
using Qualia.Domain.Suscripciones;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;
using Norte = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Norte;
using Sur = Qualia.Infrastructure.Persistence.Semillas.DatosDemo.Sur;

namespace Qualia.Tests.Integracion;

/// <summary>
/// Integración real entre el motor de llamadas (D) y el módulo de consumo (C), sin dobles:
/// reserva al iniciar, facturación al completar, liberación ante fallo definitivo y
/// bloqueo por suscripción o cuota, todo contra PostgreSQL.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class ConsumoRealIntegracionTests(PostgresFixture postgres)
{
    [FactConDocker]
    public async Task Iniciar_reserva_minutos_y_completar_los_factura()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_consumo_{Guid.NewGuid():N}", usarConsumoReal: true);
        var llamadaId = Norte.Llamadas[0];

        Assert.Equal(HttpStatusCode.Accepted,
            (await api.ClienteComo(Norte.Supervisora).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);

        var (reservado, duracion) = await ConsumoAsync(api, Norte.Empresa, llamadaId);
        var minutosEsperados = ConsumoLlamada.CalcularMinutos(duracion);
        Assert.Equal(EstadoConsumo.Reservado, reservado.Estado);
        Assert.Equal(minutosEsperados, reservado.MinutosReservados);

        await api.ProcesarColaAsync();

        var (facturado, _) = await ConsumoAsync(api, Norte.Empresa, llamadaId);
        Assert.Equal(EstadoConsumo.Facturado, facturado.Estado);
        Assert.Equal(minutosEsperados, facturado.MinutosFacturables);
        Assert.Equal(0m, facturado.MinutosReservados);

        // El AdminEmpresa ve el consumo facturado en el endpoint del módulo de suscripciones.
        var periodo = DateTime.UtcNow.ToString("yyyy-MM");
        var consumo = await QualiaApiFactory.LeerAsync<ConsumoDto>(
            await api.ClienteComo(Norte.Admin).GetAsync($"/api/consumo?periodo={periodo}"));
        Assert.Equal(minutosEsperados, consumo.MinutosFacturables);
    }

    [FactConDocker]
    public async Task Fallo_definitivo_libera_la_reserva()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_consumo_fallo_{Guid.NewGuid():N}", maximoIntentos: 1,
            s => { s.RemoveAll<ITranscriptor>(); s.AddSingleton<ITranscriptor>(new TranscriptorConFallos(int.MaxValue)); },
            usarConsumoReal: true);
        var llamadaId = Norte.Llamadas[1];

        Assert.Equal(HttpStatusCode.Accepted,
            (await api.ClienteComo(Norte.Supervisora).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null)).StatusCode);
        await api.ProcesarColaAsync();

        var (consumo, _) = await ConsumoAsync(api, Norte.Empresa, llamadaId);
        Assert.Equal(EstadoConsumo.Liberado, consumo.Estado);
        Assert.Equal(0m, consumo.MinutosReservados);
        Assert.Equal(0m, consumo.MinutosFacturables);
    }

    [FactConDocker]
    public async Task Suscripcion_en_mora_bloquea_el_inicio_sin_dejar_nada_a_medias()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_consumo_mora_{Guid.NewGuid():N}", usarConsumoReal: true);
        var llamadaId = Sur.Llamadas[0];
        await EjecutarSqlAsync(api, db => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE qualia.suscripcion SET estado = 'EnMora' WHERE empresa_id = {Sur.Empresa}"));

        var respuesta = await api.ClienteComo(Sur.Supervisor).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("SUSCRIPCION_NO_ACTIVA", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
        await AsegurarSinEfectosAsync(api, Sur.Supervisor, llamadaId);
    }

    [FactConDocker]
    public async Task Cuota_agotada_bloquea_el_inicio_sin_dejar_nada_a_medias()
    {
        await using var api = new QualiaApiFactory(postgres, $"qualia_consumo_cuota_{Guid.NewGuid():N}", usarConsumoReal: true);
        var llamadaId = Sur.Llamadas[1];
        await EjecutarSqlAsync(api, db => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE qualia.periodo_consumo SET minutos_contratados = 0.5 WHERE empresa_id = {Sur.Empresa}"));

        var respuesta = await api.ClienteComo(Sur.Supervisor).PostAsync($"/api/llamadas/{llamadaId}/evaluacion", null);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("CUOTA_AGOTADA", (await QualiaApiFactory.LeerAsync<ErrorDto>(respuesta)).Codigo);
        await AsegurarSinEfectosAsync(api, Sur.Supervisor, llamadaId);
    }

    private static async Task<(ConsumoLlamada Consumo, decimal DuracionSeg)> ConsumoAsync(QualiaApiFactory api, Guid empresa, Guid llamadaId)
    {
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var consumo = await db.ConsumosLlamada.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(c => c.EmpresaId == empresa && c.LlamadaId == llamadaId);
        var duracion = await db.Llamadas.IgnoreQueryFilters().Where(l => l.Id == llamadaId).Select(l => l.DuracionSeg).SingleAsync();
        return (consumo, duracion);
    }

    private static async Task EjecutarSqlAsync(QualiaApiFactory api, Func<QualiaDbContext, Task<int>> sql)
    {
        _ = api.Services; // Arranca la API (esquema y datos demo) antes de modificar datos.
        await using var scope = api.Services.CreateAsyncScope();
        Assert.True(await sql(scope.ServiceProvider.GetRequiredService<QualiaDbContext>()) > 0);
    }

    /// <summary>La transacción se revirtió: la llamada sigue Disponible, sin trabajo ni consumo.</summary>
    private static async Task AsegurarSinEfectosAsync(QualiaApiFactory api, Guid usuario, Guid llamadaId)
    {
        var detalle = await QualiaApiFactory.LeerAsync<LlamadaDto>(await api.ClienteComo(usuario).GetAsync($"/api/llamadas/{llamadaId}"));
        Assert.Equal(EstadoLlamada.Disponible, detalle.Estado);

        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        Assert.False(await db.TrabajosProcesamiento.IgnoreQueryFilters().AnyAsync(t => t.LlamadaId == llamadaId));
        Assert.False(await db.ConsumosLlamada.IgnoreQueryFilters().AnyAsync(c => c.LlamadaId == llamadaId));
    }
}
