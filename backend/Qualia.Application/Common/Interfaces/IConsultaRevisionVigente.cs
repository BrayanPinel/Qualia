namespace Qualia.Application.Common.Interfaces;

/// <summary>
/// CONTRATO B → D (y C): resultado humano vigente de una evaluación.
/// Lo implementa el Rol B (<c>Features/Revision/ConsultaRevisionVigente</c>) a partir de
/// Revision_humana / Criterio_revisado / Evidencia_revision.
/// D lo usa en GET /api/llamadas/{id}/evaluacion para calcular puntajeHumano y puntajeVigente
/// sin duplicar la lógica de revisión; C puede usarlo para los puntajes oficiales de scorecards.
/// </summary>

public interface IConsultaRevisionVigente
{
    /// <summary>
    /// Devuelve el último ajuste completo aplicable o null si la evaluación no tiene ajustes.
    /// </summary>

    Task<AjusteVigente?> ObtenerAjusteVigenteAsync(Guid empresaId, Guid evaluacionId, CancellationToken cancellationToken);
}

/// <param name="CriteriosPorDetalle">Clave: Detalle_evaluacion.id del resultado original de IA.</param>
public sealed record AjusteVigente(IReadOnlyDictionary<Guid, CriterioAjustado> CriteriosPorDetalle);

public sealed record CriterioAjustado(decimal Puntaje, string Justificacion, IReadOnlyList<EvidenciaAjustada> Evidencias);

public sealed record EvidenciaAjustada(Guid SegmentoId, string CitaTextual);
