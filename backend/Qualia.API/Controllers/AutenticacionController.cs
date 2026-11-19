using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Features.Autenticacion;
using Qualia.Application.Features.Autenticacion.Dtos;

namespace Qualia.API.Controllers;

/// Inicio de sesión de los usuarios de una empresa.
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AutenticacionController(InicioSesion inicioSesion) : ControllerBase
{
    /// Devuelve un token Bearer con la empresa y el rol del usuario.
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<SesionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SesionDto>> Login([FromBody] LoginDto dto, CancellationToken ct) =>
        Ok(await inicioSesion.IniciarEmpresarialAsync(dto, ct));
}

/// Inicio de sesión del Administrador global de la plataforma.
[ApiController]
[Route("api/plataforma/auth")]
[Produces("application/json")]
public sealed class PlataformaAutenticacionController(InicioSesion inicioSesion) : ControllerBase
{
    /// Devuelve un token Bearer con rol PlatformAdmin (sin empresa).
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<SesionPlataformaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SesionPlataformaDto>> Login([FromBody] LoginPlataformaDto dto, CancellationToken ct) =>
        Ok(await inicioSesion.IniciarPlataformaAsync(dto, ct));
}
