namespace Qualia.Application.Common.Interfaces;

/// CONTRATO B → D (y C): resultado humano vigente de una evaluación.
/// Lo implementa el Rol B (Features/Revision/ConsultaRevisionVigente) a partir de
/// Revision_humana / Criterio_revisado / Evidencia_revision.
/// D lo usa en GET /api/empresas/{empresaId}/llamadas/{id}/evaluacion para calcular puntajeHumano y puntajeVigente
/// sin duplicar la lógica de revisión; C puede usarlo para los puntajes oficiales de scorecards.

public interface IConsultaRevisionVigente
{
    /// Devuelve el último ajuste completo aplicable o null si la evaluación no tiene ajustes.

    Task<AjusteVigente?> ObtenerAjusteVigenteAsync(Guid empresaId, Guid evaluacionId, CancellationToken cancellationToken);
}

/// CriteriosPorDetalle: Clave: Detalle_evaluacion.id del resultado original de IA.
public sealed record AjusteVigente(IReadOnlyDictionary<Guid, CriterioAjustado> CriteriosPorDetalle);

public sealed record CriterioAjustado(decimal Puntaje, string Justificacion, IReadOnlyList<EvidenciaAjustada> Evidencias);

public sealed record EvidenciaAjustada(Guid SegmentoId, string CitaTextual);
