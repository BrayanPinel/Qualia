namespace Qualia.Application.Common.Interfaces;

/// Contrato compartido de auditoría empresarial (Rol A, consumido por B/C/D).
/// usuarioId puede ser null cuando actúa el sistema.
/// Nunca se registran contraseñas, hashes, tokens ni secretos en
/// antes o despues.
public interface IRegistroAuditoria
{
    Task RegistrarAsync(
        Guid empresaId,
        Guid? usuarioId,
        string accion,
        string entidad,
        Guid? entidadId,
        object? antes = null,
        object? despues = null,
        CancellationToken cancellationToken = default);
}
