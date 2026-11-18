using Qualia.Domain.Enums;

namespace Qualia.Application.Common.Seguridad;

/// <summary>
/// Emite los tokens de acceso. La empresa y el rol salen siempre de la base de datos,
/// nunca de lo que envía el cliente.
/// </summary>
public interface IGeneradorToken
{
    /// <summary>Sesión de un usuario de empresa: incluye el claim empresa_id.</summary>
    TokenEmitido EmitirEmpresarial(Guid usuarioId, Guid empresaId, RolEmpresa rol, string nombre);

    /// <summary>Sesión del administrador global: rol PlatformAdmin y sin empresa_id.</summary>
    TokenEmitido EmitirPlataforma(Guid administradorId, string nombre);
}

public sealed record TokenEmitido(string AccessToken, DateTime ExpiraEn);

/// <summary>Rol global que no pertenece a <see cref="RolEmpresa"/>.</summary>
public static class RolesPlataforma
{
    public const string PlatformAdmin = "PlatformAdmin";
}
