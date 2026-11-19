using Qualia.Domain.Common;

namespace Qualia.Domain.Entidades;

// Rol B — Revisión humana. Los resultados de IA (Detalle_evaluacion / Evidencia_criterio)
// nunca se modifican: cada decisión del supervisor se guarda aparte (RN-12).

/// Tabla revision_humana: una decisión de un Supervisor sobre una evaluación.
/// Una confirmación es una revisión sin criterios revisados; un ajuste completo
/// trae exactamente un CriterioRevisado por criterio de la rúbrica.
public class RevisionHumana : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EvaluacionId { get; set; }
    public Guid SupervisorId { get; set; }
    public DateTime FechaRevision { get; set; }

    /// Motivo del ajuste o comentario de la confirmación.
    public string Comentarios { get; set; } = string.Empty;

    public ICollection<CriterioRevisado> Criterios { get; set; } = [];

    public bool EsAjuste => Criterios.Count > 0;
}

/// Tabla criterio_revisado: puntaje humano de un criterio dentro de una revisión.
/// Referencia el DetalleEvaluacion original de IA, que se conserva intacto.
public class CriterioRevisado : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RevisionId { get; set; }
    public Guid DetalleId { get; set; }
    public decimal PuntajeAjustado { get; set; }

    /// Justificación del supervisor para este criterio.
    public string Observacion { get; set; } = string.Empty;

    public ICollection<EvidenciaRevision> Evidencias { get; set; } = [];
}

/// Tabla evidencia_revision: cita aportada por el supervisor. PK (empresa_id, criterio_revisado_id, segmento_id).
/// Igual que la evidencia de IA, la cita debe existir en un segmento de la misma llamada (RN-09).
public class EvidenciaRevision : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid CriterioRevisadoId { get; set; }
    public Guid SegmentoId { get; set; }
    public string CitaTextual { get; set; } = string.Empty;
}
