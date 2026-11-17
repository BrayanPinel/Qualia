using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Interfaces;

namespace Qualia.Infrastructure.Provisional;

// IMPLEMENTACIONES PROVISIONALES — NO SON PERSISTENCIA REAL.

/// <summary>
/// Reemplazar por la implementación del Rol A (tabla Auditoria).
/// Mientras tanto solo escribe en el log; nunca registra secretos.
/// </summary>
public sealed class RegistroAuditoriaProvisional(ILogger<RegistroAuditoriaProvisional> logger) : IRegistroAuditoria
{
    public Task RegistrarAsync(
        Guid empresaId,
        Guid? usuarioId,
        string accion,
        string entidad,
        Guid? entidadId,
        object? antes = null,
        object? despues = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[PROVISIONAL Rol A] Auditoría: {Accion} {Entidad} {EntidadId} por {UsuarioId} (empresa {EmpresaId})",
            accion, entidad, entidadId, usuarioId, empresaId);
        return Task.CompletedTask;
    }
}
