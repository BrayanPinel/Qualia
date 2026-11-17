namespace Qualia.Application.Features.Revision.Dtos;

// DTOs de entrada de la revisión humana. La salida es EvaluacionDto (módulo de llamadas).

/// <summary>ConfirmarEvaluacionDto { revisionEsperada }: declaración de revisión completa del supervisor.</summary>
public sealed class ConfirmarEvaluacionDto
{
    public int? RevisionEsperada { get; set; }
}

/// <summary>Ajuste COMPLETO: incluye todos los criterios de la rúbrica exactamente una vez.</summary>
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

    /// <summary>Lista vacía = ausencia explícita de evidencia, respaldada por la justificación (RN-10).</summary>
    public List<EvidenciaEntradaDto>? Evidencias { get; set; }
}

public sealed class EvidenciaEntradaDto
{
    public Guid? SegmentoId { get; set; }
    public string? CitaTextual { get; set; }
}
