using Qualia.Domain.Enums;

namespace Qualia.Application.Common.Seguridad;

/// Identidad del usuario que hace la petición, obtenida del token validado.
/// Implementación en la API (lee los claims de HttpContext.User). En el worker no hay usuario.
public interface IUsuarioActual
{
    bool EstaAutenticado { get; }
    Guid? UsuarioId { get; }
    Guid? EmpresaId { get; }

    /// Rol tal como viene en el claim (incluye "PlatformAdmin").
    string? Rol { get; }
}

/// Nombres de claims que el token debe contener.
public static class ClaimsQualia
{
    public const string EmpresaId = "empresa_id";
    public const string UsuarioId = System.Security.Claims.ClaimTypes.NameIdentifier;
    public const string Rol = System.Security.Claims.ClaimTypes.Role;
}

/// Usuario empresarial autenticado con rol válido.
public sealed record UsuarioEmpresa(Guid EmpresaId, Guid UsuarioId, RolEmpresa Rol);

public static class UsuarioActualExtensiones
{
    /// Exige un usuario empresarial autenticado (no PlatformAdmin) con alguno de los roles indicados.
    public static UsuarioEmpresa RequerirRol(this IUsuarioActual usuario, params RolEmpresa[] rolesPermitidos)
    {
        if (!usuario.EstaAutenticado || usuario.UsuarioId is null)
        {
            throw new Errores.ExcepcionNegocio(401, "NO_AUTENTICADO", "Se requiere una sesión válida.");
        }

        // Una sesión válida sin empresa (PlatformAdmin) está autenticada pero no autorizada: 403, no 401.
        if (usuario.EmpresaId is null
            || !Enum.TryParse<RolEmpresa>(usuario.Rol, ignoreCase: false, out var rol)
            || !rolesPermitidos.Contains(rol))
        {
            throw Errores.ExcepcionNegocio.Prohibido("ROL_NO_AUTORIZADO", "Su rol no permite realizar esta operación.");
        }

        return new UsuarioEmpresa(usuario.EmpresaId.Value, usuario.UsuarioId.Value, rol);
    }
}
