using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// Segmento de la transcripción. Texto contiene SIEMPRE el texto ya enmascarado (RN-14).
/// Los tiempos de la evidencia provienen de aquí, nunca del LLM (RN-09).
public class SegmentoTranscripcion : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LlamadaId { get; set; }
    public int Orden { get; set; }
    public string Hablante { get; set; } = string.Empty;
    public decimal InicioSeg { get; set; }
    public decimal FinSeg { get; set; }
    public string Texto { get; set; } = string.Empty;
}

/// Evaluación generada por IA (máximo una por llamada).
/// Si RequiereRevision es false, el resultado de la IA es oficial sin intervención humana.
/// Si es true (evidencia inválida, falta sin fragmento o nota cerca del umbral), no es oficial
/// hasta que un Supervisor la confirme o la ajuste.
/// Revision es el contador de concurrencia optimista (revisionEsperada).
public class EvaluacionIA : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LlamadaId { get; set; }
    public string ModeloUsado { get; set; } = string.Empty;
    public ModoIA ModoIA { get; set; }
    public EstadoEvaluacion Estado { get; set; } = EstadoEvaluacion.EvaluadaPorIA;
    public bool RequiereRevision { get; set; }
    public DateTime FechaEvaluacion { get; set; }
    public int Revision { get; set; } = 1;

    /// Oficial: revisada por un supervisor o calificada por la IA sin fallas detectadas.
    public bool EsOficial => Estado != EstadoEvaluacion.EvaluadaPorIA || !RequiereRevision;

    public ICollection<DetalleEvaluacion> Detalles { get; set; } = [];
}

/// Resultado de IA por criterio (uno por criterio de la rúbrica).
public class DetalleEvaluacion : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EvaluacionId { get; set; }
    public Guid CriterioId { get; set; }
    public bool CumpleCriterio { get; set; }
    public decimal PuntajeCriterio { get; set; }
    public string Observacion { get; set; } = string.Empty;

    public CriterioRubrica? Criterio { get; set; }
    public ICollection<EvidenciaCriterio> Evidencias { get; set; } = [];
}

/// Evidencia validada: la cita existe dentro del segmento indicado de la misma llamada.
/// Una lista vacía representa explícitamente la ausencia de evidencia.
public class EvidenciaCriterio : IEntidadEmpresa
{
    public Guid EmpresaId { get; set; }
    public Guid DetalleId { get; set; }
    public Guid SegmentoId { get; set; }
    public string CitaTextual { get; set; } = string.Empty;

    public SegmentoTranscripcion? Segmento { get; set; }
}
