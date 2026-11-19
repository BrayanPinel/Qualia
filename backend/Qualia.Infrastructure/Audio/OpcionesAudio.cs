namespace Qualia.Infrastructure.Audio;

/// Sección "Audio" de la configuración.
public sealed class OpcionesAudio
{
    public const string Seccion = "Audio";

    /// Secreto HMAC para firmar las URLs temporales (mínimo 32 caracteres).
    /// Si falta, se genera uno aleatorio al iniciar (las URLs dejan de servir tras reiniciar).
    /// En producción se debe definir como variable de entorno: Audio__ClaveFirma.
    public string? ClaveFirma { get; set; }

    /// Vigencia de la URL temporal.
    public TimeSpan VigenciaUrl { get; set; } = TimeSpan.FromMinutes(10);

    /// Carpeta base de los audios de demostración (relativa al directorio de la API).
    public string DirectorioBase { get; set; } = "AudiosDemo";

    /// Modo demostración: si el archivo no existe, se sirve un WAV sintético con la duración
    /// registrada para que el reproductor y el posicionamiento por segmento funcionen.
    public bool GenerarAudioSinteticoSiFalta { get; set; } = true;
}
