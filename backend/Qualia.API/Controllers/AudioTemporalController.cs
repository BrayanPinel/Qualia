using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas;

namespace Qualia.API.Controllers;

/// Destino de la URL temporal que entrega GET /api/empresas/{empresaId}/llamadas/{id}/audio.
/// validó al emitir la URL y aquí se verifica su firma HMAC y su expiración.
/// Admite peticiones Range, necesarias para posicionar el reproductor en el segmento de la evidencia.

[ApiController]
[AllowAnonymous]
[Route("api/audio-temporal")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AudioTemporalController(
    IGeneradorUrlAudio generadorUrl,
    IContextoEmpresa contextoEmpresa,
    IQualiaDbContext db,
    IAlmacenamientoAudio almacenamiento) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Descargar(string token, CancellationToken ct)
    {
        if (!generadorUrl.Validar(token, out var empresaId, out var llamadaId))
        {
            return NotFound();
        }

        // La empresa sale del token firmado; el filtro global sigue aplicándose.
        if (contextoEmpresa.EmpresaIdActual is { } actual && actual != empresaId)
        {
            return NotFound();
        }

        contextoEmpresa.Establecer(empresaId);

        var llamada = await db.Llamadas.AsNoTracking()
            .Where(l => l.Id == llamadaId)
            .Select(l => new { l.ArchivoRuta, l.DuracionSeg })
            .FirstOrDefaultAsync(ct);

        if (llamada is null)
        {
            return NotFound();
        }

        var audio = await almacenamiento.AbrirAsync(llamada.ArchivoRuta, llamada.DuracionSeg, ct);
        if (audio is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, no-store";
        return File(audio.Contenido, audio.TipoContenido, enableRangeProcessing: true);
    }
}
