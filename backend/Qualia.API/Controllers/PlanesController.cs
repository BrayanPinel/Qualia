using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Application.Features.Suscripciones.Planes;

namespace Qualia.API.Controllers;

/// Catálogo público de planes activos y sus funciones habilitadas (GET /api/planes).
[ApiController]
[Route("api/planes")]
public sealed class PlanesController : ControllerBase
{
    private readonly ListarPlanesHandler _listarPlanesHandler;

    public PlanesController(ListarPlanesHandler listarPlanesHandler)
    {
        _listarPlanesHandler = listarPlanesHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PlanDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerPlanes(CancellationToken cancellationToken)
    {
        var planes = await _listarPlanesHandler.EjecutarAsync(cancellationToken);
        return Ok(planes);
    }
}
