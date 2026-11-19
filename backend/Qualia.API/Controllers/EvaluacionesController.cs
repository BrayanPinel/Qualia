using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Application.Features.Revision;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.API.Seguridad;

namespace Qualia.API.Controllers;

/// Decisión humana sobre la propuesta de IA (Rol B, Supervisor asignado).
[ApiController]
[Authorize]
[Route(RutasEmpresa.Prefijo + "/evaluaciones")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
public sealed class EvaluacionesController(RevisionEvaluaciones revision) : ControllerBase
{
    /// Confirma la evaluación con la revisión esperada (409 si otra revisión se adelantó).
    [HttpPost("{id:guid}/confirmar")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaluacionDto>> Confirmar(Guid id, [FromBody] ConfirmarEvaluacionDto dto, CancellationToken ct) =>
        Ok(await revision.ConfirmarAsync(id, dto, ct));

    /// Ajuste completo con motivo, puntajes, justificaciones y evidencias; conserva el resultado de IA.
    [HttpPut("{id:guid}/ajuste")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<EvaluacionDto>> Ajustar(Guid id, [FromBody] AjustarEvaluacionDto dto, CancellationToken ct) =>
        Ok(await revision.AjustarAsync(id, dto, ct));
}
