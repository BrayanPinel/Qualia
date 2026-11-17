using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qualia.Application.Comun.Persistencia;
using Qualia.Application.Comun.Seguridad;

namespace Qualia.API.Seguridad;

/// <summary>
/// ESQUEMA PROVISIONAL, SOLO DESARROLLO (Autenticacion:ModoDemo = true y entorno Development). (PARA PRUEBAS)
/// TEMPORAL REMPLAZAR POR COMPAÑERO DE LA PARTE A
/// </summary>

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
