using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Resultados;

namespace Qualia.API.Common;

/// <summary>
/// Extensiones de presentación HTTP para traducir <see cref="Resultado{T}"/> al contrato
/// uniforme <see cref="ErrorDto"/>. La empresa de la petición la resuelve IContextoEmpresa
/// a partir del token, sin pasos adicionales en los controladores.
/// </summary>
public static class ResultadoExtensions
{
    private static readonly string[] ClaimsAdministrador =
    [
        "administradorId",
        "administrador_id",
        ClaimTypes.NameIdentifier,
        "sub",
    ];

    public static IActionResult AActionResult<T>(this ControllerBase controller, Resultado<T> resultado)
    {
        if (resultado.EsExitoso)
        {
            return controller.Ok(resultado.Valor);
        }

        return controller.AErrorActionResult(resultado.Error);
    }

    public static IActionResult AErrorActionResult(this ControllerBase controller, ErrorAplicacion error)
    {
        var traceId = Activity.Current?.Id ?? controller.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
        var dto = error.AErrorDto(traceId);

        return new ObjectResult(dto)
        {
            StatusCode = dto.Status,
        };
    }

    public static Guid ObtenerAdministradorIdDesdeClaims(this ControllerBase controller)
    {
        var user = controller.HttpContext?.User;
        if (user is null)
        {
            return Guid.Empty;
        }

        foreach (var tipoClaim in ClaimsAdministrador)
        {
            var valor = user.FindFirst(tipoClaim)?.Value;
            if (Guid.TryParse(valor, out var adminId) && adminId != Guid.Empty)
            {
                return adminId;
            }
        }

        return Guid.Empty;
    }
}
