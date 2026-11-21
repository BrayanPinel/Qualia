using Qualia.Application.Features.Llamadas.Dtos;

namespace Qualia.Application.Features.Llamadas;

/// Emite y valida URLs temporales y firmadas para reproducir el audio de una llamada.
/// La URL no contiene la ruta interna del archivo.
public interface IGeneradorUrlAudio
{
    AccesoAudioDto Generar(Guid empresaId, Guid llamadaId, string urlBase);

    /// Valida firma y expiración. Devuelve false si el token fue alterado o expiró.
    bool Validar(string token, out Guid empresaId, out Guid llamadaId);
}

/// Acceso al contenido binario del audio (almacenamiento local, blob, etc.).
public interface IAlmacenamientoAudio
{
    /// Devuelve null si el archivo no existe.
    Task<ContenidoAudio?> AbrirAsync(string archivoRuta, decimal duracionSeg, CancellationToken cancellationToken);
}

/// Escritura de audios cargados por la empresa. Separado de IAlmacenamientoAudio porque
/// la lectura la usan el reproductor y el transcriptor, y la escritura solo la carga.
public interface IGuardadoAudio
{
    Task GuardarAsync(string archivoRuta, Stream contenido, CancellationToken cancellationToken);

    /// Limpieza cuando la llamada no llegó a registrarse. No falla si el archivo no existe.
    Task EliminarAsync(string archivoRuta, CancellationToken cancellationToken);
}

/// Lee la duración real desde la cabecera del archivo: con ella se reservan y facturan los minutos.
/// Devuelve null si el formato no se reconoce o el archivo está dañado.
public interface IMedidorDuracionAudio
{
    decimal? Medir(ReadOnlySpan<byte> contenido, string extension);
}

/// EsSintetico: true si no existe la grabación y se generó un audio de relleno.
public sealed record ContenidoAudio(Stream Contenido, string TipoContenido, bool EsSintetico = false);
