using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Common;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;

namespace Qualia.API.Controllers;

/// <summary>
/// Consulta de la suscripción vigente y procesamiento de pagos simulados de la empresa autenticada
/// (<c>GET /api/suscripcion</c> y <c>POST /api/suscripcion/pago-simulado</c>, fase1 §7.2 y §7.8).
/// </summary>
[ApiController]
[Route("api/suscripcion")]
[Authorize(Roles = "AdminEmpresa")]
public sealed class SuscripcionController : ControllerBase
{
    private readonly ObtenerSuscripcionHandler _obtenerSuscripcionHandler;
    private readonly RegistrarPagoSimuladoHandler _registrarPagoSimuladoHandler;
    private readonly IContextoEmpresa _contextoEmpresa;

    public SuscripcionController(
        ObtenerSuscripcionHandler obtenerSuscripcionHandler,
        RegistrarPagoSimuladoHandler registrarPagoSimuladoHandler,
        IContextoEmpresa contextoEmpresa)
    {
        _obtenerSuscripcionHandler = obtenerSuscripcionHandler;
        _registrarPagoSimuladoHandler = registrarPagoSimuladoHandler;
        _contextoEmpresa = contextoEmpresa;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SuscripcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerSuscripcion(CancellationToken cancellationToken)
    {
        this.SincronizarContextoEmpresaDesdeClaims(_contextoEmpresa);
        var resultado = await _obtenerSuscripcionHandler.EjecutarAsync(cancellationToken);
        return this.AActionResult(resultado);
    }

    [HttpPost("pago-simulado")]
    [ProducesResponseType(typeof(SuscripcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarPagoSimulado(
        [FromBody] PagoSimuladoDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return this.AErrorActionResult(ErrorAplicacion.Validacion(new Dictionary<string, string[]>
            {
                ["resultado"] = ["El cuerpo de la solicitud es obligatorio."],
            }));
        }

        this.SincronizarContextoEmpresaDesdeClaims(_contextoEmpresa);
        var resultado = await _registrarPagoSimuladoHandler.EjecutarAsync(request, cancellationToken);
        return this.AActionResult(resultado);
    }
}
