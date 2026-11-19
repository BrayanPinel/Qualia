using Qualia.Domain.Enums;

namespace Qualia.Application.Common.Seguridad;

/// Emite los tokens de acceso. La empresa y el rol salen siempre de la base de datos,
/// nunca de lo que envía el cliente.
public interface IGeneradorToken
{
    /// Sesión de un usuario de empresa: incluye el claim empresa_id.
    TokenEmitido EmitirEmpresarial(Guid usuarioId, Guid empresaId, RolEmpresa rol, string nombre);

    /// Sesión del administrador global: rol PlatformAdmin y sin empresa_id.
    TokenEmitido EmitirPlataforma(Guid administradorId, string nombre);
}

public sealed record TokenEmitido(string AccessToken, DateTime ExpiraEn);

/// Rol global que no pertenece a RolEmpresa.
public static class RolesPlataforma
{
    public const string PlatformAdmin = "PlatformAdmin";
}
