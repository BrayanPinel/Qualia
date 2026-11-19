using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Common.Errores;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.API.Seguridad;

namespace Qualia.API.Controllers;

/// Endpoints del proceso principal de llamadas.
/// El controlador solo traduce HTTP; permisos, reglas y persistencia están en Application.

[ApiController]
[Authorize]
[Route(RutasEmpresa.Prefijo + "/llamadas")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class LlamadasController(ConsultasLlamadas consultas, IniciarEvaluacion iniciarEvaluacion) : ControllerBase
{
    /// Lista las llamadas visibles para el usuario (AdminEmpresa, Supervisor asignado, Agente propietario).
    [HttpGet]
    [ProducesResponseType<PaginaDto<LlamadaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<LlamadaDto>>> Listar([FromQuery] FiltroLlamadasDto filtro, CancellationToken ct) =>
        Ok(await consultas.ListarAsync(filtro, ct));

    /// Detalle y estado de una llamada.
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LlamadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LlamadaDto>> Obtener(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAsync(id, ct));

    /// Transcripción enmascarada, ordenada por segmento.
    [HttpGet("{id:guid}/transcripcion")]
    [ProducesResponseType<IReadOnlyList<SegmentoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SegmentoDto>>> Transcripcion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerTranscripcionAsync(id, ct));

    /// URL temporal y firmada para reproducir el audio. El acceso queda auditado.
    [HttpGet("{id:guid}/audio")]
    [ProducesResponseType<AccesoAudioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccesoAudioDto>> Audio(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAccesoAudioAsync(id, $"{Request.Scheme}://{Request.Host}{Request.PathBase}", ct));

    /// Evaluación con puntajes (IA, humano y vigente), justificación y evidencia por criterio.
    [HttpGet("{id:guid}/evaluacion")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluacionDto>> Evaluacion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerEvaluacionAsync(id, ct));

    /// El Supervisor asignado inicia la evaluación: reserva minutos y encola el procesamiento.
    [HttpPost("{id:guid}/evaluacion")]
    [ProducesResponseType<EvaluacionAceptadaDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluacionAceptadaDto>> IniciarEvaluacion(Guid id, CancellationToken ct)
    {
        var aceptada = await iniciarEvaluacion.EjecutarAsync(id, ct);
        return Accepted(aceptada.SeguimientoUrl, aceptada);
    }
}
