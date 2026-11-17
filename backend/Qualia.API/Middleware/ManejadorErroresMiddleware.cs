using System.Diagnostics;
using Qualia.Application.Common.Errores;
using Qualia.Domain.Common;

namespace Qualia.API.Middleware;

/// <summary>
/// Middleware global de errores: toda excepción se responde con el formato uniforme ErrorDto
/// </summary>

public sealed class ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (Exception ex) when (!contexto.Response.HasStarted)
        {
            var traceId = Activity.Current?.Id ?? contexto.TraceIdentifier;

            var error = ex switch
            {
                ExcepcionNegocio n => new ErrorDto(n.Status, n.Codigo, n.Message, n.Errores, traceId),
                ReglaDominioException d => new ErrorDto(StatusCodes.Status409Conflict, d.Codigo, d.Message, null, traceId),
                OperationCanceledException when contexto.RequestAborted.IsCancellationRequested => null,
                _ => new ErrorDto(StatusCodes.Status500InternalServerError, "ERROR_INTERNO",
                    "Ocurrió un error inesperado.", null, traceId),
            };

            if (error is null)
            {
                return; // El cliente canceló la petición.
            }

            if (error.Status >= 500)
            {
                logger.LogError(ex, "Error no controlado ({TraceId})", traceId);
            }

            contexto.Response.Clear();
            contexto.Response.StatusCode = error.Status;
            await contexto.Response.WriteAsJsonAsync(error);
        }
    }
}
