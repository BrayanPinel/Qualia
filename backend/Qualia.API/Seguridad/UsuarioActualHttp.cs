using Qualia.Application.Comun.Seguridad;

namespace Qualia.API.Seguridad;

/// <summary>Lee la identidad de los claims del token ya validado.</summary>

public sealed class UsuarioActualHttp(IHttpContextAccessor accesor) : IUsuarioActual
{
    private System.Security.Claims.ClaimsPrincipal? Principal => accesor.HttpContext?.User;

    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UsuarioId => LeerGuid(ClaimsQualia.UsuarioId);

    public Guid? EmpresaId => LeerGuid(ClaimsQualia.EmpresaId);

    public string? Rol => EstaAutenticado ? Principal!.FindFirst(ClaimsQualia.Rol)?.Value : null;

    private Guid? LeerGuid(string tipo) =>
        EstaAutenticado && Guid.TryParse(Principal!.FindFirst(tipo)?.Value, out var valor) ? valor : null;
}
