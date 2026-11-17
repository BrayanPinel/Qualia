using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Common;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;
using Qualia.Application.Features.Suscripciones.Plataforma.ListarEmpresasComerciales;

namespace Qualia.API.Controllers;

/// <summary>
/// Administración comercial global para <c>PlatformAdmin</c>, sin acceso al contenido
/// de llamadas (<c>GET /api/plataforma/empresas</c> y
/// <c>PUT /api/plataforma/empresas/{empresaId}/suscripcion</c>, fase1 §7.2 y HU-07).
/// </summary>
[ApiController]
[Route("api/plataforma/empresas")]
[Authorize(Roles = "PlatformAdmin")]
public sealed class PlataformaEmpresasController : ControllerBase
{
    private readonly ListarEmpresasComercialesHandler _listarHandler;
    private readonly CambiarSuscripcionHandler _cambiarSuscripcionHandler;

    public PlataformaEmpresasController(
        ListarEmpresasComercialesHandler listarHandler,
        CambiarSuscripcionHandler cambiarSuscripcionHandler)
    {
        _listarHandler = listarHandler;
        _cambiarSuscripcionHandler = cambiarSuscripcionHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<EmpresaComercialDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListarEmpresas(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _listarHandler.EjecutarAsync(
            new PaginacionQuery(pagina, tamanoPagina),
            cancellationToken);

        return this.AActionResult(resultado);
    }

    [HttpPut("{empresaId:guid}/suscripcion")]
    [ProducesResponseType(typeof(SuscripcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarSuscripcion(
        [FromRoute] Guid empresaId,
        [FromBody] CambiarSuscripcionDto? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return this.AErrorActionResult(ErrorAplicacion.Validacion(new Dictionary<string, string[]>
            {
                ["datos"] = ["El cuerpo de la solicitud es obligatorio."],
            }));
        }

        var adminId = this.ObtenerAdministradorIdDesdeClaims();
        var resultado = await _cambiarSuscripcionHandler.EjecutarAsync(
            new CambiarSuscripcionCommand(empresaId, adminId, request),
            cancellationToken);

        return this.AActionResult(resultado);
    }
}
