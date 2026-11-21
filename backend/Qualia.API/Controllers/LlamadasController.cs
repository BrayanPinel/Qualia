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
[Tags("Llamadas")]
public sealed class LlamadasController(
    ConsultasLlamadas consultas,
    IniciarEvaluacion iniciarEvaluacion,
    CargarLlamada cargarLlamada) : ControllerBase
{
    /// Carga una grabación propia (nota de voz de WhatsApp, MP3, WAV o M4A) para una campaña y un agente.
    /// Puede hacerlo el AdminEmpresa o un Supervisor asignado a la campaña. La duración se mide en el servidor
    /// y la llamada queda Disponible: el worker la encola y la evalúa automáticamente.
    /// Herramienta de pruebas: responde 403 CARGA_MANUAL_DESHABILITADA salvo que
    /// Procesamiento:CargaManualHabilitada sea true (activo en Development).
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CargarLlamada.TamanoMaximoBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargarLlamada.TamanoMaximoBytes + 1024 * 1024)]
    [EndpointSummary("Cargar una grabación")]
    [ProducesResponseType<LlamadaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LlamadaDto>> Cargar([FromForm] CargarLlamadaFormulario formulario, CancellationToken ct)
    {
        await using var contenido = formulario.Audio?.OpenReadStream() ?? Stream.Null;
        var llamada = await cargarLlamada.EjecutarAsync(
            new CargarLlamadaSolicitud(
                formulario.CampanaId, formulario.AgenteId, formulario.FechaLlamada,
                formulario.Audio?.FileName ?? string.Empty, contenido),
            ct);

        return Created($"/api/empresas/{llamada.EmpresaId}/llamadas/{llamada.Id}", llamada);
    }

    /// Lista las llamadas visibles para el usuario (AdminEmpresa, Supervisor asignado, Agente propietario).
    [HttpGet]
    [EndpointSummary("Listar llamadas")]
    [ProducesResponseType<PaginaDto<LlamadaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<LlamadaDto>>> Listar([FromQuery] FiltroLlamadasDto filtro, CancellationToken ct) =>
        Ok(await consultas.ListarAsync(filtro, ct));

    /// Detalle y estado de una llamada.
    [HttpGet("{id:guid}")]
    [EndpointSummary("Detalle y estado de la llamada")]
    [ProducesResponseType<LlamadaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LlamadaDto>> Obtener(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAsync(id, ct));

    /// Transcripción enmascarada, ordenada por segmento.
    [HttpGet("{id:guid}/transcripcion")]
    [EndpointSummary("Transcripción enmascarada")]
    [ProducesResponseType<IReadOnlyList<SegmentoDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SegmentoDto>>> Transcripcion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerTranscripcionAsync(id, ct));

    /// URL temporal y firmada para reproducir el audio. El acceso queda auditado.
    [HttpGet("{id:guid}/audio")]
    [EndpointSummary("URL firmada del audio")]
    [ProducesResponseType<AccesoAudioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccesoAudioDto>> Audio(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAccesoAudioAsync(id, $"{Request.Scheme}://{Request.Host}{Request.PathBase}", ct));

    /// Evaluación con puntajes (IA, humano y vigente), justificación y evidencia por criterio.
    [HttpGet("{id:guid}/evaluacion")]
    [EndpointSummary("Evaluación de la llamada")]
    [ProducesResponseType<EvaluacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluacionDto>> Evaluacion(Guid id, CancellationToken ct) =>
        Ok(await consultas.ObtenerEvaluacionAsync(id, ct));

    /// El Supervisor asignado inicia la evaluación: reserva minutos y encola el procesamiento.
    [HttpPost("{id:guid}/evaluacion")]
    [EndpointSummary("Iniciar evaluación con IA")]
    [ProducesResponseType<EvaluacionAceptadaDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorDto>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EvaluacionAceptadaDto>> IniciarEvaluacion(Guid id, CancellationToken ct)
    {
        var aceptada = await iniciarEvaluacion.EjecutarAsync(id, ct);
        return Accepted(aceptada.SeguimientoUrl, aceptada);
    }
}

/// Cuerpo multipart de la carga. Vive en la API porque IFormFile es un detalle de HTTP.
public sealed class CargarLlamadaFormulario
{
    /// Archivo de audio (.ogg, .opus, .mp3, .wav o .m4a), máximo 25 MB y 30 minutos.
    public IFormFile? Audio { get; set; }

    public Guid CampanaId { get; set; }

    public Guid AgenteId { get; set; }

    /// Opcional: cuándo ocurrió la llamada. Si falta, se usa el momento de la carga.
    public DateTime? FechaLlamada { get; set; }
}
