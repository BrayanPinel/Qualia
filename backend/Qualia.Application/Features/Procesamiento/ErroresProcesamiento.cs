using Qualia.Domain.Comun;

namespace Qualia.Application.Features.Procesamiento;

/// <summary>
/// Error esperado del procesamiento. Su mensaje debe ser seguro para mostrarse al usuario
/// (se guarda en Trabajo_procesamiento.ultimo_error y se expone como motivoFallo).
/// Se reintenta hasta el máximo configurado.
/// </summary>
public class ErrorProcesamiento(string mensajeSeguro) : Exception(mensajeSeguro);

/// <summary>Error que no se soluciona reintentando: el trabajo falla de inmediato.</summary>
public sealed class ErrorProcesamientoPermanente(string mensajeSeguro) : ErrorProcesamiento(mensajeSeguro);

/// <summary>
/// Convierte cualquier excepción en un mensaje saneado: nunca expone trazas, rutas
/// internas, cadenas de conexión ni respuestas crudas de proveedores.
/// </summary>
public static class SaneadorErrores
{
    public const int LongitudMaxima = 500;

    public static string Sanear(Exception excepcion)
    {
        var mensaje = excepcion switch
        {
            ErrorProcesamiento e => e.Message,
            ReglaDominioException => "El estado de la llamada no permite continuar el procesamiento.",
            TimeoutException or TaskCanceledException => "El proveedor de IA no respondió a tiempo.",
            HttpRequestException => "El proveedor de IA no está disponible temporalmente.",
            _ => "Error interno durante el procesamiento.",
        };

        return mensaje.Length <= LongitudMaxima ? mensaje : mensaje[..LongitudMaxima];
    }

    public static bool EsPermanente(Exception excepcion) =>
        excepcion is ErrorProcesamientoPermanente or ReglaDominioException;
}
