using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Campanas;
using Qualia.Application.Features.Campanas.Dtos;

namespace Qualia.API.Controllers;

/// <summary>
/// Campañas, asignaciones de usuarios y rúbricas de una campaña (Rol B).
/// El controlador solo traduce HTTP; permisos, reglas y persistencia están en Application.
/// </summary>
[ApiController]
[Authorize]
[Route("api/campanas")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class CampanasController(GestionCampanas campanas, GestionRubricas rubricas) : ControllerBase
{
    /// <summary>Campañas visibles según el rol (AdminEmpresa/Auditor: todas; Supervisor/Agente: las suyas).</summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<CampanaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<CampanaDto>>> Listar([FromQuery] PaginacionDto paginacion, CancellationToken ct) =>
        Ok(await campanas.ListarAsync(paginacion, ct));

    /// <summary>Crea una campaña (AdminEmpresa).</summary>
    [HttpPost]
    [ProducesResponseType<CampanaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CampanaDto>> Crear([FromBody] GuardarCampanaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await campanas.CrearAsync(dto, ct));

    /// <summary>Actualiza nombre y estado de una campaña (AdminEmpresa).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] GuardarCampanaDto dto, CancellationToken ct)
    {
        await campanas.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    /// <summary>Usuarios asignados a la campaña (AdminEmpresa).</summary>
    [HttpGet("{id:guid}/usuarios")]
    [ProducesResponseType<PaginaDto<UsuarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaDto<UsuarioDto>>> ListarUsuarios(Guid id, [FromQuery] PaginacionDto paginacion, CancellationToken ct) =>
        Ok(await campanas.ListarUsuariosAsync(id, paginacion, ct));

    /// <summary>Asigna un Agente o Supervisor de la empresa a la campaña. Idempotente.</summary>
    [HttpPut("{id:guid}/usuarios/{usuarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AsignarUsuario(Guid id, Guid usuarioId, CancellationToken ct)
    {
        await campanas.AsignarUsuarioAsync(id, usuarioId, ct);
        return NoContent();
    }

    /// <summary>Revoca la asignación del usuario a la campaña. Idempotente.</summary>
    [HttpDelete("{id:guid}/usuarios/{usuarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevocarUsuario(Guid id, Guid usuarioId, CancellationToken ct)
    {
        await campanas.RevocarUsuarioAsync(id, usuarioId, ct);
        return NoContent();
    }

    /// <summary>Versiones de rúbrica de la campaña (AdminEmpresa o Supervisor asignado).</summary>
    [HttpGet("{id:guid}/rubricas")]
    [ProducesResponseType<IReadOnlyList<RubricaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RubricaDto>>> ListarRubricas(Guid id, CancellationToken ct) =>
        Ok(await rubricas.ListarPorCampanaAsync(id, ct));

    /// <summary>Crea una nueva versión de rúbrica en Borrador (AdminEmpresa).</summary>
    [HttpPost("{id:guid}/rubricas")]
    [ProducesResponseType<RubricaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> CrearRubrica(Guid id, [FromBody] CrearRubricaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await rubricas.CrearAsync(id, dto, ct));
}
