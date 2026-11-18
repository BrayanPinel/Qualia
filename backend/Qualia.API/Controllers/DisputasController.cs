using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Disputas;
using Qualia.Application.Features.Disputas.Dtos;

namespace Qualia.API.Controllers;

/// <summary>Disputas de evaluaciones: el agente las abre y el supervisor asignado las resuelve.</summary>
[ApiController]
[Authorize]
[Route("api")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class DisputasController(GestionDisputas disputas) : ControllerBase
{
    /// <summary>El agente propietario disputa una evaluación oficial (plan con disputas).</summary>
    [HttpPost("evaluaciones/{id:guid}/disputas")]
    [ProducesResponseType<DisputaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisputaDto>> Crear(Guid id, [FromBody] CrearDisputaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await disputas.CrearAsync(id, dto, ct));

    /// <summary>Disputas visibles según el rol, con estado opcional (Abierta o Resuelta).</summary>
    [HttpGet("disputas")]
    [ProducesResponseType<PaginaDto<DisputaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<DisputaDto>>> Listar([FromQuery] FiltroDisputasDto filtro, CancellationToken ct) =>
        Ok(await disputas.ListarAsync(filtro, ct));

    /// <summary>El supervisor asignado mantiene o modifica las notas con justificación.</summary>
    [HttpPut("disputas/{id:guid}/resolucion")]
    [ProducesResponseType<DisputaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DisputaDto>> Resolver(Guid id, [FromBody] ResolverDisputaDto dto, CancellationToken ct) =>
        Ok(await disputas.ResolverAsync(id, dto, ct));
}
