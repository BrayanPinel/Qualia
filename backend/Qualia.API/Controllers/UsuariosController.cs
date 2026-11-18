using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Application.Features.Usuarios;
using Qualia.Application.Features.Usuarios.Dtos;

namespace Qualia.API.Controllers;

/// <summary>Usuarios de la empresa de la sesión (solo AdminEmpresa).</summary>
[ApiController]
[Authorize]
[Route("api/usuarios")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class UsuariosController(GestionUsuarios usuarios) : ControllerBase
{
    /// <summary>Usuarios de la empresa, paginados y ordenados por nombre.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<UsuarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<UsuarioDto>>> Listar([FromQuery] PaginacionDto paginacion, CancellationToken ct) =>
        Ok(await usuarios.ListarAsync(paginacion, ct));

    /// <summary>Crea un usuario de la empresa (AdminEmpresa, Supervisor, Agente o Auditor).</summary>
    [HttpPost]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Crear([FromBody] CrearUsuarioDto dto, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await usuarios.CrearAsync(dto, ct));

    /// <summary>Cambia nombre y estado. No permite dejar a la empresa sin AdminEmpresa activo.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarUsuarioDto dto, CancellationToken ct)
    {
        await usuarios.ActualizarAsync(id, dto, ct);
        return NoContent();
    }
}
