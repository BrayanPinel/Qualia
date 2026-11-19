using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Common;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Common.Errores;
using Qualia.API.Seguridad;

namespace Qualia.API.Controllers;

/// Consulta de la suscripción vigente y procesamiento de pagos simulados de la empresa autenticada
/// (GET /api/empresas/{empresaId}/suscripcion y POST /api/empresas/{empresaId}/suscripcion/pago-simulado).
[ApiController]
[Route(RutasEmpresa.Prefijo + "/suscripcion")]
[Authorize(Roles = "AdminEmpresa")]
public sealed class SuscripcionController : ControllerBase
{
    private readonly ObtenerSuscripcionHandler _obtenerSuscripcionHandler;
    private readonly RegistrarPagoSimuladoHandler _registrarPagoSimuladoHandler;

    public SuscripcionController(
        ObtenerSuscripcionHandler obtenerSuscripcionHandler,
        RegistrarPagoSimuladoHandler registrarPagoSimuladoHandler)
    {
        _obtenerSuscripcionHandler = obtenerSuscripcionHandler;
        _registrarPagoSimuladoHandler = registrarPagoSimuladoHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SuscripcionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerSuscripcion(CancellationToken cancellationToken)
    {
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
        var resultado = await _registrarPagoSimuladoHandler.EjecutarAsync(request, cancellationToken);
        return this.AActionResult(resultado);
    }
}
