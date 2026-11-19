namespace Qualia.Application.Features.Revision.Dtos;

// DTOs de entrada de la revisión humana. La salida es EvaluacionDto (módulo de llamadas).

/// ConfirmarEvaluacionDto { revisionEsperada }: declaración de revisión completa del supervisor.
public sealed class ConfirmarEvaluacionDto
{
    public int? RevisionEsperada { get; set; }
}

/// Ajuste COMPLETO: incluye todos los criterios de la rúbrica exactamente una vez.
public sealed class AjustarEvaluacionDto
{
    public int? RevisionEsperada { get; set; }
    public string? Motivo { get; set; }
    public List<AjusteCriterioDto>? Criterios { get; set; }
}

public sealed class AjusteCriterioDto
{
    public Guid? CriterioId { get; set; }
    public decimal? Puntaje { get; set; }
    public string? Justificacion { get; set; }

    /// Lista vacía = ausencia explícita de evidencia, respaldada por la justificación (RN-10).
    public List<EvidenciaEntradaDto>? Evidencias { get; set; }
}

public sealed class EvidenciaEntradaDto
{
    public Guid? SegmentoId { get; set; }
    public string? CitaTextual { get; set; }
}
