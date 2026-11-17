using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Un ciclo del worker: busca trabajos disponibles, los reclama de forma atómica
/// y procesa cada uno en su propio scope con el contexto de empresa del trabajo.
/// El worker (BackgroundService) solo llama a <see cref="EjecutarCicloAsync"/> en bucle;
/// las pruebas lo invocan directamente.
/// </summary>
public sealed class EjecutorProcesamiento(
    IServiceScopeFactory scopes,
    TimeProvider reloj,
    IOptions<OpcionesProcesamiento> opciones,
    ILogger<EjecutorProcesamiento> logger)
{
    private readonly OpcionesProcesamiento _opciones = opciones.Value;

    /// <summary>Devuelve la cantidad de trabajos procesados en el ciclo.</summary>
    public async Task<int> EjecutarCicloAsync(CancellationToken cancellationToken)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var candidatos = await BuscarCandidatosAsync(ahora, cancellationToken);
        var procesados = 0;

        foreach (var candidato in candidatos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var scope = scopes.CreateAsyncScope();
            // El worker no tiene HttpContext: la empresa sale del trabajo persistido.
            scope.ServiceProvider.GetRequiredService<IContextoEmpresa>().Establecer(candidato.EmpresaId);

            if (!await ReclamarAsync(scope.ServiceProvider, candidato.Id, cancellationToken))
            {
                continue; // Otra instancia lo reclamó primero.
            }

            await scope.ServiceProvider.GetRequiredService<ProcesadorTrabajo>().ProcesarAsync(candidato.Id, cancellationToken);
            procesados++;
        }

        if (procesados > 0)
        {
            logger.LogDebug("Ciclo de procesamiento: {Cantidad} trabajo(s)", procesados);
        }

        return procesados;
    }

    private async Task<List<Candidato>> BuscarCandidatosAsync(DateTime ahora, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IQualiaDbContext>();

        // ÚNICO punto donde se ignoran los filtros por empresa: el despachador debe ver la cola
        // de todas las empresas. Solo lee (EmpresaId, Id); el procesamiento se hace ya filtrado.
        return await db.TrabajosProcesamiento
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(EstaDisponible(ahora))
            .OrderBy(t => t.ProximoIntento ?? DateTime.MinValue)
            .Take(_opciones.TamanoLote)
            .Select(t => new Candidato(t.EmpresaId, t.Id))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Reclamo atómico (UPDATE condicionado): pasa a EnProceso, incrementa intentos y fija la
    /// expiración del bloqueo. Si dos workers compiten, solo uno obtiene la fila.
    /// </summary>
    private async Task<bool> ReclamarAsync(IServiceProvider servicios, Guid trabajoId, CancellationToken ct)
    {
        var db = servicios.GetRequiredService<IQualiaDbContext>();
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var expiraBloqueo = ahora + _opciones.DuracionBloqueo;

        var filas = await db.TrabajosProcesamiento
            .Where(t => t.Id == trabajoId)
            .Where(EstaDisponible(ahora))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Estado, EstadoTrabajo.EnProceso)
                .SetProperty(t => t.Intentos, t => t.Intentos + 1)
                .SetProperty(t => t.ProximoIntento, expiraBloqueo), ct);

        return filas == 1;
    }

    /// <summary>
    /// Pendiente / ReintentoPendiente cuya espera ya venció, o EnProceso cuyo bloqueo expiró
    /// (el proceso que lo tenía se detuvo).
    /// </summary>
    private static Expression<Func<TrabajoProcesamiento, bool>> EstaDisponible(DateTime ahora) => t =>
        ((t.Estado == EstadoTrabajo.Pendiente || t.Estado == EstadoTrabajo.ReintentoPendiente)
         && (t.ProximoIntento == null || t.ProximoIntento <= ahora))
        || (t.Estado == EstadoTrabajo.EnProceso && t.ProximoIntento != null && t.ProximoIntento <= ahora);

    private sealed record Candidato(Guid EmpresaId, Guid Id);
}
