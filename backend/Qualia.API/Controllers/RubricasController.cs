using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Campanas;
using Qualia.Application.Features.Campanas.Dtos;

namespace Qualia.API.Controllers;

/// <summary>Edición de borradores y publicación de versiones de rúbrica (Rol B, AdminEmpresa).</summary>
[ApiController]
[Authorize]
[Route("api/rubricas")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
public sealed class RubricasController(GestionRubricas rubricas) : ControllerBase
{
    /// <summary>Reemplaza nombre y criterios. Solo una rúbrica en Borrador (409 si ya fue publicada).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] CrearRubricaDto dto, CancellationToken ct)
    {
        await rubricas.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>Publica el borrador (pesos = 100) y retira la versión activa anterior de la campaña.</summary>
    [HttpPost("{id:guid}/publicar")]
    [ProducesResponseType<RubricaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> Publicar(Guid id, CancellationToken ct) =>
        Ok(await rubricas.PublicarAsync(id, ct));
}
