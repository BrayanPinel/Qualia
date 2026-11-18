using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas.Dtos;

// DTOs de salida.

public sealed record LlamadaDto(
    Guid Id,
    Guid EmpresaId,
    Guid CampanaId,
    Guid AgenteId,
    Guid RubricaId,
    int VersionRubrica,
    decimal DuracionSeg,
    EstadoLlamada Estado,
    DateTime FechaLlamada,
    string? MotivoFallo,
    bool? RequiereRevision);

public sealed record SegmentoDto(
    Guid Id,
    Guid EmpresaId,
    int Orden,
    string Hablante,
    decimal InicioSeg,
    decimal FinSeg,
    string TextoRedactado);

public sealed record AccesoAudioDto(string Url, DateTime ExpiraEn);

public sealed record EvaluacionAceptadaDto(Guid LlamadaId, Guid EmpresaId, EstadoLlamada Estado, string SeguimientoUrl);

public sealed record EvaluacionDto(
    Guid Id,
    Guid EmpresaId,
    Guid LlamadaId,
    EstadoEvaluacion Estado,
    int Revision,
    ModoIA ModoIA,
    string ModeloUsado,
    bool RequiereRevision,
    bool Oficial,
    decimal PuntajeTotal,
    IReadOnlyList<ResultadoCriterioDto> Criterios);

public sealed record ResultadoCriterioDto(
    Guid CriterioId,
    string Nombre,
    decimal Peso,
    decimal PuntajeIA,
    decimal? PuntajeHumano,
    decimal PuntajeVigente,
    decimal UmbralCumplimiento,
    bool Cumple,
    string Justificacion,
    bool FalloCritico,
    IReadOnlyList<EvidenciaDto> Evidencias);

/// <summary>
/// Fragmento que sustenta la nota de un criterio. Si el criterio no se cumple, es el fragmento
/// donde el agente cometió la falta: cita, texto completo del segmento, hablante e instante del audio.
/// </summary>
public sealed record EvidenciaDto(
    Guid SegmentoId,
    string CitaTextual,
    decimal InicioSeg,
    decimal FinSeg,
    string Hablante,
    string TextoSegmento);

/// <summary>Parámetros de GET /api/llamadas (paginación y filtros opcionales).</summary>
public sealed class FiltroLlamadasDto
{
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 20;
    public Guid? CampanaId { get; set; }
    public Guid? AgenteId { get; set; }

    public string? Estado { get; set; }

    /// <summary>true: bandeja del supervisor (evaluaciones con falla de la IA pendientes de revisión).</summary>
    public bool? RequiereRevision { get; set; }
}
