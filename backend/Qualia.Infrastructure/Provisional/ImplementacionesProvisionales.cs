using Microsoft.Extensions.Logging;
using Qualia.Application.Comun.Contratos;

namespace Qualia.Infrastructure.Provisional;

// IMPLEMENTACIONES PROVISIONALES — NO SON PERSISTENCIA REAL.

/// <summary>Reemplazar por la implementación del Rol C (Suscripcion / Periodo_consumo / Consumo_llamada).</summary>
public sealed class ConsumoEvaluacionProvisional(ILogger<ConsumoEvaluacionProvisional> logger) : IConsumoEvaluacion
{
    public Task ReservarAsync(Guid empresaId, Guid llamadaId, decimal duracionSeg, CancellationToken cancellationToken)
    {
        logger.LogWarning("[PROVISIONAL Rol C] Reserva de consumo NO persistida para la llamada {LlamadaId} ({Duracion} s)", llamadaId, duracionSeg);
        return Task.CompletedTask;
    }

    public Task FacturarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken)
    {
        logger.LogWarning("[PROVISIONAL Rol C] Facturación NO persistida para la llamada {LlamadaId}", llamadaId);
        return Task.CompletedTask;
    }

    public Task LiberarAsync(Guid empresaId, Guid llamadaId, UsoTecnicoIA uso, CancellationToken cancellationToken)
    {
        logger.LogWarning("[PROVISIONAL Rol C] Liberación NO persistida para la llamada {LlamadaId}", llamadaId);
        return Task.CompletedTask;
    }
}

/// <summary>Reemplazar por la implementación del Rol A (tabla Auditoria).</summary>
public sealed class RegistroAuditoriaProvisional(ILogger<RegistroAuditoriaProvisional> logger) : IRegistroAuditoria
{
    public Task RegistrarAsync(EntradaAuditoria entrada, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[PROVISIONAL Rol A] Auditoría: {Accion} {Entidad} {EntidadId} por {UsuarioId} (empresa {EmpresaId})",
            entrada.Accion, entrada.Entidad, entrada.EntidadId, entrada.UsuarioId, entrada.EmpresaId);
        return Task.CompletedTask;
    }
}

/// <summary>Reemplazar por la implementación del Rol B (Revision_humana / Criterio_revisado / Evidencia_revision).</summary>
public sealed class ConsultaRevisionVigenteProvisional : IConsultaRevisionVigente
{
    public Task<AjusteVigente?> ObtenerAjusteVigenteAsync(Guid empresaId, Guid evaluacionId, CancellationToken cancellationToken) =>
        Task.FromResult<AjusteVigente?>(null);
}
