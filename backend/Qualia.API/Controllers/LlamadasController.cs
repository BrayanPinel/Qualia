using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qualia.Application.Comun.Errores;
using Qualia.Application.Comun.Paginacion;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Llamadas.Dtos;

namespace Qualia.API.Controllers;

/// <summary>
/// Endpoints del proceso principal de llamadas.
/// El controlador solo traduce HTTP; permisos, reglas y persistencia están en Application.
/// </summary>

[ApiController]
[Authorize]
[Route("api/llamadas")]
[Produces("application/json")]
[ProducesResponseType<ErrorDto>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ErrorDto>(StatusCodes.Status403Forbidden)]
public sealed class LlamadasController(ConsultasLlamadas consultas, IniciarEvaluacion iniciarEvaluacion) : ControllerBase
{
    /// <summary>Lista las llamadas visibles para el usuario (AdminEmpresa, Supervisor asignado, Agente propietario).</summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<LlamadaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<LlamadaDto>>> Listar([FromQuery] FiltroLlamadasDto filtro, CancellationToken ct) =>
        Ok(await consultas.ListarAsync(filtro, ct));

    /// <summary>Detalle y estado de una llamada.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LlamadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LlamadaDto>> Obtener(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAsync(id, ct));

    /// <summary>Transcripción enmascarada, ordenada por segmento.</summary>
    [HttpGet("{id:guid}/transcripcion")]
    [ProducesResponseType<IReadOnlyList<SegmentoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SegmentoDto>>> Transcripcion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerTranscripcionAsync(id, ct));

    /// <summary>URL temporal y firmada para reproducir el audio. El acceso queda auditado.</summary>
    [HttpGet("{id:guid}/audio")]
    [ProducesResponseType<AccesoAudioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccesoAudioDto>> Audio(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAccesoAudioAsync(id, $"{Request.Scheme}://{Request.Host}{Request.PathBase}", ct));

    /// <summary>Evaluación con puntajes (IA, humano y vigente), justificación y evidencia por criterio.</summary>
    [HttpGet("{id:guid}/evaluacion")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluacionDto>> Evaluacion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerEvaluacionAsync(id, ct));

    /// <summary>El Supervisor asignado inicia la evaluación: reserva minutos y encola el procesamiento.</summary>
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
