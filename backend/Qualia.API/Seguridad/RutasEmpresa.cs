using Microsoft.AspNetCore.Mvc.Filters;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Seguridad;

namespace Qualia.API.Seguridad;

/// Rutas de los recursos de una empresa: /api/empresas/{empresaId}/…
/// La empresa de la URL solo se compara con la de la sesión; los datos se siguen filtrando por la
/// empresa del token (filtro global de QualiaDbContext), nunca por la de la URL.
public static class RutasEmpresa
{
    public const string ParametroEmpresa = "empresaId";
    public const string Prefijo = "api/empresas/{" + ParametroEmpresa + ":guid}";

    public const string CodigoEmpresaNoEncontrada = "EMPRESA_NO_ENCONTRADA";
}

/// Filtro global para toda acción cuya ruta empieza con RutasEmpresa.Prefijo.
/// Se ejecuta antes del enlace de modelo: una empresa ajena responde 404 aunque el cuerpo sea inválido.
/// Sesión sin empresa (PlatformAdmin) en una ruta de empresa: 403. Sin sesión: lo resuelve [Authorize] (401).
public sealed class FiltroEmpresaDeRuta(IUsuarioActual usuarioActual) : IAsyncResourceFilter
{
    public Task OnResourceExecutionAsync(ResourceExecutingContext contexto, ResourceExecutionDelegate siguiente)
    {
        var plantilla = contexto.ActionDescriptor.AttributeRouteInfo?.Template;
        if (plantilla is null
            || !plantilla.StartsWith(RutasEmpresa.Prefijo, StringComparison.OrdinalIgnoreCase)
            || !usuarioActual.EstaAutenticado)
        {
            return siguiente();
        }

        if (usuarioActual.EmpresaId is not { } empresaSesion)
        {
            throw ExcepcionNegocio.Prohibido("ROL_NO_AUTORIZADO", "Su rol no permite realizar esta operación.");
        }

        var valor = contexto.RouteData.Values[RutasEmpresa.ParametroEmpresa]?.ToString();
        if (!Guid.TryParse(valor, out var empresaRuta) || empresaRuta != empresaSesion)
        {
            throw ExcepcionNegocio.NoEncontrado(RutasEmpresa.CodigoEmpresaNoEncontrada, "No se encontró la empresa.");
        }

        return siguiente();
    }
}
