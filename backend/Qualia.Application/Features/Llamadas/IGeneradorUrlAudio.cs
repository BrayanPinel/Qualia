using Qualia.Application.Features.Llamadas.Dtos;

namespace Qualia.Application.Features.Llamadas;

/// <summary>
/// Emite y valida URLs temporales y firmadas para reproducir el audio de una llamada.
/// La URL no contiene la ruta interna del archivo.
/// </summary>
public interface IGeneradorUrlAudio
{
    AccesoAudioDto Generar(Guid empresaId, Guid llamadaId, string urlBase);

    /// <summary>Valida firma y expiración. Devuelve false si el token fue alterado o expiró.</summary>
    bool Validar(string token, out Guid empresaId, out Guid llamadaId);
}

/// <summary>Acceso al contenido binario del audio (almacenamiento local, blob, etc.).</summary>
public interface IAlmacenamientoAudio
{
    /// <returns>null si el archivo no existe.</returns>
    Task<ContenidoAudio?> AbrirAsync(string archivoRuta, decimal duracionSeg, CancellationToken cancellationToken);
}

public sealed record ContenidoAudio(Stream Contenido, string TipoContenido);
