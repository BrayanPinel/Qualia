using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Campanas;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.API.Seguridad;

namespace Qualia.API.Controllers;

/// Campañas, asignaciones de usuarios y rúbricas de una campaña.
/// El controlador solo traduce HTTP; permisos, reglas y persistencia están en Application.
[ApiController]
[Authorize]
[Route(RutasEmpresa.Prefijo + "/campanas")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class CampanasController(GestionCampanas campanas, GestionRubricas rubricas) : ControllerBase
{
    /// Campañas visibles según el rol (AdminEmpresa/Auditor: todas; Supervisor/Agente: las suyas).
    [HttpGet]
    [ProducesResponseType<PaginaDto<CampanaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<CampanaDto>>> Listar([FromQuery] PaginacionDto paginacion, CancellationToken ct) =>
        Ok(await campanas.ListarAsync(paginacion, ct));

    /// Crea una campaña (AdminEmpresa).
    [HttpPost]
    [ProducesResponseType<CampanaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CampanaDto>> Crear([FromBody] GuardarCampanaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await campanas.CrearAsync(dto, ct));

    /// Actualiza nombre y estado de una campaña (AdminEmpresa).
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] GuardarCampanaDto dto, CancellationToken ct)
    {
        await campanas.ActualizarAsync(id, dto, ct);
        return NoContent();
    }

    /// Usuarios asignados a la campaña (AdminEmpresa).
    [HttpGet("{id:guid}/usuarios")]
    [ProducesResponseType<PaginaDto<UsuarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaDto<UsuarioDto>>> ListarUsuarios(Guid id, [FromQuery] PaginacionDto paginacion, CancellationToken ct) =>
        Ok(await campanas.ListarUsuariosAsync(id, paginacion, ct));

    /// Asigna un Agente o Supervisor de la empresa a la campaña. Idempotente.
    [HttpPut("{id:guid}/usuarios/{usuarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AsignarUsuario(Guid id, Guid usuarioId, CancellationToken ct)
    {
        await campanas.AsignarUsuarioAsync(id, usuarioId, ct);
        return NoContent();
    }

    /// Revoca la asignación del usuario a la campaña. Idempotente.
    [HttpDelete("{id:guid}/usuarios/{usuarioId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevocarUsuario(Guid id, Guid usuarioId, CancellationToken ct)
    {
        await campanas.RevocarUsuarioAsync(id, usuarioId, ct);
        return NoContent();
    }

    /// Versiones de rúbrica de la campaña (AdminEmpresa o Supervisor asignado).
    [HttpGet("{id:guid}/rubricas")]
    [ProducesResponseType<IReadOnlyList<RubricaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RubricaDto>>> ListarRubricas(Guid id, CancellationToken ct) =>
        Ok(await rubricas.ListarPorCampanaAsync(id, ct));

    /// Crea una nueva versión de rúbrica en Borrador (AdminEmpresa).
    [HttpPost("{id:guid}/rubricas")]
    [ProducesResponseType<RubricaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> CrearRubrica(Guid id, [FromBody] CrearRubricaDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await rubricas.CrearAsync(id, dto, ct));
}
