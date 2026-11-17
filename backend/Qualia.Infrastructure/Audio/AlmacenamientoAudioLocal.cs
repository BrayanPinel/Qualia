using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Qualia.Application.Features.Llamadas;

namespace Qualia.Infrastructure.Audio;

/// <summary>
/// Almacenamiento local de audios de demostración. Protege contra rutas que intenten salir
/// de la carpeta base. En modo demostración genera un WAV sintético si el archivo no existe.
/// </summary>
public sealed class AlmacenamientoAudioLocal(IOptions<OpcionesAudio> opciones, IHostEnvironment entorno) : IAlmacenamientoAudio
{
    private const int MuestrasPorSegundo = 8000;
    private const decimal DuracionMaximaSintetica = 1800m;

    private readonly OpcionesAudio _opciones = opciones.Value;

    public Task<ContenidoAudio?> AbrirAsync(string archivoRuta, decimal duracionSeg, CancellationToken cancellationToken)
    {
        var baseDir = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, _opciones.DirectorioBase));
        var ruta = Path.GetFullPath(Path.Combine(baseDir, archivoRuta));

        if (!ruta.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<ContenidoAudio?>(null);
        }

        if (File.Exists(ruta))
        {
            Stream archivo = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            return Task.FromResult<ContenidoAudio?>(new ContenidoAudio(archivo, TipoContenido(ruta)));
        }

        if (!_opciones.GenerarAudioSinteticoSiFalta)
        {
            return Task.FromResult<ContenidoAudio?>(null);
        }

        return Task.FromResult<ContenidoAudio?>(new ContenidoAudio(GenerarWav(duracionSeg), "audio/wav", EsSintetico: true));
    }

    private static string TipoContenido(string ruta) => Path.GetExtension(ruta).ToLowerInvariant() switch
    {
        ".mp3" => "audio/mpeg",
        ".wav" => "audio/wav",
        ".ogg" => "audio/ogg",
        ".m4a" => "audio/mp4",
        _ => "application/octet-stream",
    };

    /// <summary>WAV PCM 8 kHz / 8 bits mono con tonos suaves alternos (simula turnos de habla).</summary>
    private static MemoryStream GenerarWav(decimal duracionSeg)
    {
        var segundos = Math.Clamp(duracionSeg, 1m, DuracionMaximaSintetica);
        var muestras = (int)(segundos * MuestrasPorSegundo);
        var datos = new byte[muestras];

        for (var i = 0; i < muestras; i++)
        {
            var t = (double)i / MuestrasPorSegundo;
            var frecuencia = ((int)(t / 4) % 2 == 0) ? 330.0 : 440.0;
            datos[i] = (byte)(128 + 20 * Math.Sin(2 * Math.PI * frecuencia * t));
        }

        var flujo = new MemoryStream(44 + muestras);
        using (var w = new BinaryWriter(flujo, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            w.Write("RIFF"u8);
            w.Write(36 + muestras);
            w.Write("WAVE"u8);
            w.Write("fmt "u8);
            w.Write(16);                       // tamaño del bloque fmt
            w.Write((short)1);                 // PCM
            w.Write((short)1);                 // mono
            w.Write(MuestrasPorSegundo);
            w.Write(MuestrasPorSegundo);       // bytes por segundo
            w.Write((short)1);                 // alineación de bloque
            w.Write((short)8);                 // bits por muestra
            w.Write("data"u8);
            w.Write(muestras);
            w.Write(datos);
        }

        flujo.Position = 0;
        return flujo;
    }
}
