using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qualia.Application.Common.Errores;

namespace Qualia.API.Configuracion;

/// Convierte los errores de enlace de modelo de [ApiController] (JSON mal formado, campos desconocidos,
/// tipos inválidos, cuerpo ausente) en ErrorDto 400 VALIDACION, para que el cliente reciba
/// un único formato de error. Los mensajes se reescriben para no exponer nombres de tipos internos.
internal static class RespuestaModeloInvalido
{
    public static IActionResult Crear(ActionContext contexto)
    {
        var entradas = contexto.ModelState.Where(e => e.Value is { Errors.Count: > 0 }).ToList();

        // Si el JSON no se pudo leer, el parámetro del cuerpo también aparece como "requerido": se omite ese ruido.
        var hayErroresJson = entradas.Any(e => e.Key.StartsWith('$'));

        var errores = new Dictionary<string, string[]>();
        foreach (var (clave, estado) in entradas)
        {
            if (hayErroresJson && !clave.StartsWith('$'))
            {
                continue;
            }

            errores[NormalizarClave(clave)] = estado!.Errors.Select(Traducir).Distinct().ToArray();
        }

        var traceId = Activity.Current?.Id ?? contexto.HttpContext.TraceIdentifier;
        var error = new ErrorDto(StatusCodes.Status400BadRequest, "VALIDACION", "Uno o más campos no son válidos.", errores, traceId);

        return new BadRequestObjectResult(error);
    }

    private static string NormalizarClave(string clave) => clave switch
    {
        "" or "$" => "cuerpo",
        _ when clave.StartsWith("$.") => clave[2..],
        _ => clave,
    };

    private static string Traducir(ModelError error)
    {
        var mensaje = error.ErrorMessage;
        if (mensaje.Contains("could not be mapped", StringComparison.OrdinalIgnoreCase))
        {
            return "Campo no permitido.";
        }

        if (mensaje.Contains("field is required", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("non-empty request body", StringComparison.OrdinalIgnoreCase))
        {
            return "El cuerpo de la petición es obligatorio.";
        }

        if (error.Exception is not null || mensaje.Contains("JSON", StringComparison.OrdinalIgnoreCase))
        {
            return "El valor no tiene un formato válido.";
        }

        return string.IsNullOrWhiteSpace(mensaje) ? "Valor no válido." : mensaje;
    }
}
