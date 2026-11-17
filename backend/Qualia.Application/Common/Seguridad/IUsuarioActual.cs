using Qualia.Domain.Enums;

namespace Qualia.Application.Common.Seguridad;

/// <summary>
/// Identidad del usuario que hace la petición, obtenida del token validado.
/// Implementación en la API (lee los claims de HttpContext.User). En el worker no hay usuario.
/// </summary>
public interface IUsuarioActual
{
    bool EstaAutenticado { get; }
    Guid? UsuarioId { get; }
    Guid? EmpresaId { get; }

    /// <summary>Rol tal como viene en el claim (incluye "PlatformAdmin").</summary>
    string? Rol { get; }
}

/// <summary>
/// Nombres de claims que el token debe contener.
/// </summary>
public static class ClaimsQualia
{
    public const string EmpresaId = "empresa_id";
    public const string UsuarioId = System.Security.Claims.ClaimTypes.NameIdentifier;
    public const string Rol = System.Security.Claims.ClaimTypes.Role;
}

/// <summary>Usuario empresarial autenticado con rol válido.</summary>
public sealed record UsuarioEmpresa(Guid EmpresaId, Guid UsuarioId, RolEmpresa Rol);

public static class UsuarioActualExtensiones
{
    /// <summary>
    /// Exige un usuario empresarial autenticado (no PlatformAdmin) con alguno de los roles indicados.
    /// </summary>
    public static UsuarioEmpresa RequerirRol(this IUsuarioActual usuario, params RolEmpresa[] rolesPermitidos)
    {
        if (!usuario.EstaAutenticado || usuario.UsuarioId is null || usuario.EmpresaId is null)
        {
            throw new Errores.ExcepcionNegocio(401, "NO_AUTENTICADO", "Se requiere una sesión válida.");
        }

        if (!Enum.TryParse<RolEmpresa>(usuario.Rol, ignoreCase: false, out var rol) || !rolesPermitidos.Contains(rol))
        {
            throw Errores.ExcepcionNegocio.Prohibido("ROL_NO_AUTORIZADO", "Su rol no permite realizar esta operación.");
        }

        return new UsuarioEmpresa(usuario.EmpresaId.Value, usuario.UsuarioId.Value, rol);
    }
}
