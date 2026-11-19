using System.Text.RegularExpressions;
using Microsoft.IdentityModel.JsonWebTokens;
using Qualia.API.Seguridad;
using Qualia.Application.Common.Seguridad;

namespace Qualia.Tests.Integracion.Infraestructura;

/// Cliente de pruebas: convierte "/api/{recurso de empresa}/…" en "/api/empresas/{empresaId}/…" usando la
/// empresa de la sesión de la petición (cabecera X-Usuario-Demo o claim empresa_id del Bearer).
/// Las rutas que ya empiezan con /api/empresas/ y las globales (planes, auth, plataforma…) no se tocan.
/// Sin sesión o sin empresa (PlatformAdmin) se usa Guid.Empty: la API responde 401 o 403 igual que antes.
public sealed partial class PrefijoEmpresaHandler(Func<Guid, Guid> empresaDeUsuario) : DelegatingHandler
{
    [GeneratedRegex(@"^/api/(usuarios|campanas|rubricas|llamadas|evaluaciones|disputas|agentes|reportes|suscripcion|consumo)(/|$)")]
    private static partial Regex RutaCorta();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken ct)
    {
        var uri = peticion.RequestUri!;
        if (RutaCorta().IsMatch(uri.AbsolutePath))
        {
            var ruta = $"/api/empresas/{EmpresaDeLaSesion(peticion)}/{uri.AbsolutePath["/api/".Length..]}";
            peticion.RequestUri = new UriBuilder(uri) { Path = ruta }.Uri;
        }

        return base.SendAsync(peticion, ct);
    }

    private Guid EmpresaDeLaSesion(HttpRequestMessage peticion)
    {
        if (peticion.Headers.TryGetValues(AutenticacionDemoHandler.Cabecera, out var demo)
            && Guid.TryParse(demo.FirstOrDefault(), out var usuarioId))
        {
            return empresaDeUsuario(usuarioId);
        }

        if (peticion.Headers.Authorization is { Scheme: "Bearer", Parameter: { } token })
        {
            try
            {
                return new JsonWebToken(token).TryGetPayloadValue<string>(ClaimsQualia.EmpresaId, out var empresa)
                       && Guid.TryParse(empresa, out var id) ? id : Guid.Empty;
            }
            catch (ArgumentException)
            {
                return Guid.Empty;   // token deliberadamente mal formado: la API responderá 401
            }
        }

        return Guid.Empty;
    }
}
