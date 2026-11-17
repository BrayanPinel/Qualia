namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// Contrato compartido de auditoría empresarial (Rol A, consumido por B/C/D).
/// <paramref name="usuarioId"/> puede ser null cuando actúa el sistema.
/// Nunca se registran contraseñas, hashes, tokens ni secretos en
/// <paramref name="antes"/> o <paramref name="despues"/>.
/// </summary>
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
