using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Qualia.Infrastructure.Audio;

namespace Qualia.Infrastructure.IA;

/// <summary>
/// Ground data de una llamada demo: qué actor habla en cada intervalo.
/// Lo genera herramientas/GenerarAudiosDemo.cs junto al audio (*.ground.json).
/// </summary>
public sealed record GroundData(string Archivo, string? Descripcion, decimal DuracionSeg, IReadOnlyList<SegmentoGround> Segmentos);

public sealed record SegmentoGround(int Orden, string Actor, decimal InicioSeg, decimal FinSeg, string Texto);

/// <summary>Lee los archivos *.ground.json que acompañan a los audios de demostración.</summary>
public sealed class LectorGroundData(IOptions<OpcionesAudio> opciones, IHostEnvironment entorno)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string DirectorioAudios => Path.GetFullPath(Path.Combine(entorno.ContentRootPath, opciones.Value.DirectorioBase));

    /// <returns>null si la llamada no tiene ground data.</returns>
    public GroundData? Leer(string archivoRuta)
    {
        var rutaAudio = Path.GetFullPath(Path.Combine(DirectorioAudios, archivoRuta));
        if (!rutaAudio.StartsWith(DirectorioAudios + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rutaGround = Path.ChangeExtension(rutaAudio, ".ground.json");
        return File.Exists(rutaGround) ? JsonSerializer.Deserialize<GroundData>(File.ReadAllText(rutaGround), Json) : null;
    }

    /// <summary>Todos los ground data disponibles.</summary>
    public IReadOnlyList<GroundData> LeerTodos() =>
        Directory.Exists(DirectorioAudios)
            ? Directory.GetFiles(DirectorioAudios, "*.ground.json", SearchOption.AllDirectories)
                .Order()
                .Select(r => JsonSerializer.Deserialize<GroundData>(File.ReadAllText(r), Json)!)
                .ToList()
            : [];
}
