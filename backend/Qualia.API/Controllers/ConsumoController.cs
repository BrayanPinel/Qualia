using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Common;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Common.Errores;
using Qualia.API.Seguridad;

namespace Qualia.API.Controllers;

/// Consulta de minutos contratados, reservados, facturables, disponibles y excedentes
/// por periodo mensual UTC (GET /api/empresas/{empresaId}/consumo?periodo=YYYY-MM).
[ApiController]
[Route(RutasEmpresa.Prefijo + "/consumo")]
[Authorize(Roles = "AdminEmpresa")]
[Tags("Suscripción y consumo")]
public sealed class ConsumoController : ControllerBase
{
    private readonly ConsultarConsumoHandler _consultarConsumoHandler;

    public ConsumoController(
        ConsultarConsumoHandler consultarConsumoHandler)
    {
        _consultarConsumoHandler = consultarConsumoHandler;
    }

    [HttpGet]
    [EndpointSummary("Consumo mensual de minutos")]
    [ProducesResponseType(typeof(ConsumoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarConsumo(
        [FromQuery] string? periodo,
        CancellationToken cancellationToken)
    {
        var resultado = await _consultarConsumoHandler.EjecutarAsync(
            new ConsultarConsumoQuery(periodo),
            cancellationToken);

        return this.AActionResult(resultado);
    }
}
