using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Qualia.Infrastructure.IA;

/// Guarda las respuestas JSON crudas de los proveedores de IA en
/// RespuestasIA/{empresaId}/{llamadaId}/{fecha}_{etapa}.json.
/// Permite auditar qué devolvió Deepgram/Groq y reprocesar sin volver a pagar.
/// La respuesta cruda de Deepgram contiene la transcripción sin enmascarar (RN-14):
/// la carpeta está excluida de Git y se puede desactivar con IA:GuardarRespuestasCrudas = false.
public sealed class AlmacenRespuestasIA(
    IOptions<OpcionesIA> opciones,
    IHostEnvironment entorno,
    TimeProvider reloj,
    ILogger<AlmacenRespuestasIA> logger)
{
    private readonly OpcionesIA _opciones = opciones.Value;

    public string DirectorioBase => Path.GetFullPath(Path.Combine(entorno.ContentRootPath, _opciones.DirectorioRespuestas));

    /// Si no puede escribir, solo lo registra en el log.
    public async Task GuardarAsync(Guid empresaId, Guid llamadaId, string etapa, string contenidoJson, CancellationToken ct)
    {
        if (!_opciones.GuardarRespuestasCrudas)
        {
            return;
        }

        try
        {
            var carpeta = Path.Combine(DirectorioBase, empresaId.ToString(), llamadaId.ToString());
            Directory.CreateDirectory(carpeta);

            var marca = reloj.GetUtcNow().UtcDateTime.ToString("yyyyMMdd'T'HHmmssfff");
            var ruta = Path.Combine(carpeta, $"{marca}_{etapa}.json");
            await File.WriteAllTextAsync(ruta, contenidoJson, new UTF8Encoding(false), ct);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "No se pudo guardar la respuesta cruda {Etapa} de la llamada {LlamadaId}", etapa, llamadaId);
        }
    }
}
