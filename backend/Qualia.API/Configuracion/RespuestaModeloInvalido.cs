using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;

namespace Qualia.API.Configuracion;

/// <summary>
/// Convierte los errores de enlace de modelo de [ApiController] en <see cref="ErrorDto"/> (400 VALIDACION),
/// para que el cliente reciba un único formato de error (fase1 §7.10).
/// </summary>
internal static class RespuestaModeloInvalido
{
    public static IActionResult Crear(ActionContext contexto)
    {
        var errores = contexto.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => string.IsNullOrEmpty(e.Key) ? "cuerpo" : e.Key,
                e => e.Value!.Errors
                    .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Valor no válido." : x.ErrorMessage)
                    .ToArray());

        var traceId = Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier;
        var error = new ErrorDto(StatusCodes.Status400BadRequest, "VALIDACION", "Uno o más campos no son válidos.", errores, traceId);

        return new BadRequestObjectResult(error);
    }
}
