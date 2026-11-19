using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;

namespace Qualia.API.Seguridad;

/// Esquema auxiliar SOLO de desarrollo (Autenticacion:ModoDemo = true y entorno Development):
/// identifica al usuario por la cabecera X-Usuario-Demo. Lo usan las pruebas de integración y los .http.
/// Convive con JWT: ConfiguracionAutenticacion lo elige solo si llega esa cabecera.

public sealed class AutenticacionDemoHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> opciones,
    ILoggerFactory logger,
    UrlEncoder codificador,
    IQualiaDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, codificador)
{
    public const string Esquema = "Demo";
    public const string Cabecera = "X-Usuario-Demo";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Cabecera, out var valor))
        {
            return AuthenticateResult.NoResult();
        }

        if (!Guid.TryParse(valor.ToString(), out var usuarioId))
        {
            return AuthenticateResult.Fail("Cabecera de usuario demo inválida.");
        }

        // Búsqueda de identidad previa a conocer la empresa: única consulta que ignora el filtro.
        var usuario = await db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo, Context.RequestAborted);

        if (usuario is null)
        {
            return AuthenticateResult.Fail("Usuario demo inexistente o inactivo.");
        }

        Claim[] claims =
        [
            new(ClaimsQualia.UsuarioId, usuario.Id.ToString()),
            new(ClaimsQualia.EmpresaId, usuario.EmpresaId.ToString()),
            new(ClaimsQualia.Rol, usuario.Rol.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
        ];

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Esquema));
    }
}
