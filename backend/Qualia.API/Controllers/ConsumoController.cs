using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.API.Common;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

namespace Qualia.API.Controllers;

/// <summary>
/// Consulta de minutos contratados, reservados, facturables, disponibles y excedentes
/// por periodo mensual UTC (<c>GET /api/consumo?periodo=YYYY-MM</c>, fase1 §7.2 y §7.8).
/// </summary>
[ApiController]
[Route("api/consumo")]
[Authorize(Roles = "AdminEmpresa")]
public sealed class ConsumoController : ControllerBase
{
    private readonly ConsultarConsumoHandler _consultarConsumoHandler;
    private readonly IContextoEmpresa _contextoEmpresa;

    public ConsumoController(
        ConsultarConsumoHandler consultarConsumoHandler,
        IContextoEmpresa contextoEmpresa)
    {
        _consultarConsumoHandler = consultarConsumoHandler;
        _contextoEmpresa = contextoEmpresa;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConsumoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarConsumo(
        [FromQuery] string? periodo,
        CancellationToken cancellationToken)
    {
        this.SincronizarContextoEmpresaDesdeClaims(_contextoEmpresa);
        var resultado = await _consultarConsumoHandler.EjecutarAsync(
            new ConsultarConsumoQuery(periodo),
            cancellationToken);

        return this.AActionResult(resultado);
    }
}
