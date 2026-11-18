using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Encola sin intervención humana las llamadas Disponible cuya rúbrica ya está publicada.
/// Cada llamada se encola en su propio scope con el contexto de empresa de la llamada.
/// Si la empresa no tiene suscripción activa o cuota, sus llamadas quedan Disponible
/// y no se vuelven a intentar hasta que pase <see cref="OpcionesProcesamiento.EsperaTrasRechazoConsumo"/>.
/// </summary>
public sealed class DespachadorEvaluacionAutomatica(
    IServiceScopeFactory scopes,
    TimeProvider reloj,
    IOptions<OpcionesProcesamiento> opciones,
    ILogger<DespachadorEvaluacionAutomatica> logger)
{
    private readonly OpcionesProcesamiento _opciones = opciones.Value;
    private readonly ConcurrentDictionary<Guid, DateTime> _empresasEnEspera = new();

    /// <summary>Devuelve la cantidad de llamadas encoladas en el ciclo.</summary>
    public async Task<int> EjecutarCicloAsync(CancellationToken cancellationToken)
    {
        if (!_opciones.EvaluacionAutomatica)
        {
            return 0;
        }

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var enEspera = EmpresasEnEspera(ahora);
        var candidatos = await BuscarCandidatosAsync(enEspera, cancellationToken);
        var encoladas = 0;

        foreach (var candidato in candidatos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (enEspera.Contains(candidato.EmpresaId))
            {
                continue; // La empresa se rechazó antes en este mismo lote.
            }

            await using var scope = scopes.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IContextoEmpresa>().Establecer(candidato.EmpresaId);

            try
            {
                await scope.ServiceProvider.GetRequiredService<EncoladorEvaluacion>()
                    .EncolarAsync(candidato.EmpresaId, candidato.LlamadaId, candidato.DuracionSeg, cancellationToken);
                encoladas++;
            }
            catch (ExcepcionNegocio ex) when (ex.Codigo == "EVALUACION_YA_INICIADA")
            {
                // Un supervisor u otra instancia la encoló primero.
            }
            catch (ExcepcionNegocio ex)
            {
                // Sin suscripción activa, sin cuota u otro rechazo de consumo: se pausa la empresa.
                _empresasEnEspera[candidato.EmpresaId] = ahora + _opciones.EsperaTrasRechazoConsumo;
                enEspera.Add(candidato.EmpresaId);
                logger.LogInformation(
                    "Evaluación automática pausada para la empresa {EmpresaId}: {Codigo}",
                    candidato.EmpresaId, ex.Codigo);
            }
        }

        if (encoladas > 0)
        {
            logger.LogInformation("Evaluación automática: {Cantidad} llamada(s) encolada(s)", encoladas);
        }

        return encoladas;
    }

    private HashSet<Guid> EmpresasEnEspera(DateTime ahora)
    {
        foreach (var (empresaId, hasta) in _empresasEnEspera)
        {
            if (hasta <= ahora)
            {
                _empresasEnEspera.TryRemove(empresaId, out _);
            }
        }

        return [.. _empresasEnEspera.Keys];
    }

    private async Task<List<Candidato>> BuscarCandidatosAsync(HashSet<Guid> excluidas, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IQualiaDbContext>();
        var lista = excluidas.ToList();

        // Se ignoran los filtros por empresa porque el despachador revisa todas las empresas.
        // Solo lee identificadores; el encolado se hace después con el contexto de cada empresa.
        return await db.Llamadas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l => l.Estado == EstadoLlamada.Disponible
                        && l.Rubrica!.Estado != EstadoRubrica.Borrador
                        && !lista.Contains(l.EmpresaId))
            .OrderBy(l => l.FechaRegistro)
            .ThenBy(l => l.Id)
            .Take(_opciones.TamanoLote)
            .Select(l => new Candidato(l.EmpresaId, l.Id, l.DuracionSeg))
            .ToListAsync(ct);
    }

    private sealed record Candidato(Guid EmpresaId, Guid LlamadaId, decimal DuracionSeg);
}
