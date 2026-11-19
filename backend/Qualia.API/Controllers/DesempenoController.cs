using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Desempeno;
using Qualia.Application.Features.Desempeno.Dtos;

namespace Qualia.API.Controllers;

/// Scorecards de agentes y reporte agregado por campaña.
[ApiController]
[Authorize]
[Route("api")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
public sealed class DesempenoController(ConsultasDesempeno desempeno) : ControllerBase
{
    /// Scorecard propio del agente en una campaña y mes (YYYY-MM; por defecto el mes en curso).
    [HttpGet("agentes/me/scorecard")]
    [ProducesResponseType<ScorecardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScorecardDto>> Propio([FromQuery] FiltroScorecardDto filtro, CancellationToken ct) =>
        Ok(await desempeno.ScorecardPropioAsync(filtro, ct));

    /// Scorecard de un agente (AdminEmpresa o Supervisor asignado a la campaña).
    [HttpGet("agentes/{id:guid}/scorecard")]
    [ProducesResponseType<ScorecardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScorecardDto>> DeAgente(Guid id, [FromQuery] FiltroScorecardDto filtro, CancellationToken ct) =>
        Ok(await desempeno.ScorecardAgenteAsync(id, filtro, ct));

    /// Scorecards de los agentes de una campaña, paginados.
    [HttpGet("campanas/{id:guid}/scorecards")]
    [ProducesResponseType<PaginaDto<ScorecardDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaDto<ScorecardDto>>> DeCampana(Guid id, [FromQuery] FiltroScorecardsCampanaDto filtro, CancellationToken ct) =>
        Ok(await desempeno.ScorecardsCampanaAsync(id, filtro, ct));

    /// Reporte agregado de la campaña (AdminEmpresa, Supervisor asignado o Auditor).
    [HttpGet("reportes/campanas/{id:guid}")]
    [ProducesResponseType<ReporteCampanaDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteCampanaDto>> Reporte(Guid id, [FromQuery] FiltroReporteDto filtro, CancellationToken ct) =>
        Ok(await desempeno.ReporteCampanaAsync(id, filtro, ct));
}
