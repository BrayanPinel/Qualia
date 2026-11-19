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

/// EsSintetico: true si no existe la grabación y se generó un audio de relleno.
public sealed record ContenidoAudio(Stream Contenido, string TipoContenido, bool EsSintetico = false);
