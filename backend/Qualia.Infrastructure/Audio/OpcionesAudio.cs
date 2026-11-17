namespace Qualia.Infrastructure.Audio;

/// <summary>Sección "Audio" de la configuración.</summary>
public sealed class OpcionesAudio
{
    public const string Seccion = "Audio";

    /// <summary>
    /// Secreto HMAC para firmar las URLs temporales (mínimo 32 caracteres).
    /// Si falta, se genera uno aleatorio al iniciar (las URLs dejan de servir tras reiniciar).
    /// En producción se debe definir como variable de entorno: Audio__ClaveFirma.
    /// </summary>
    public string? ClaveFirma { get; set; }

    /// <summary>Vigencia de la URL temporal.</summary>
    public TimeSpan VigenciaUrl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Carpeta base de los audios de demostración (relativa al directorio de la API).</summary>
    public string DirectorioBase { get; set; } = "AudiosDemo";

    /// <summary>
    /// Modo demostración: si el archivo no existe, se sirve un WAV sintético con la duración
    /// registrada para que el reproductor y el posicionamiento por segmento funcionen.
    /// </summary>
    public bool GenerarAudioSinteticoSiFalta { get; set; } = true;
}
