using Qualia.Domain.Common;
using Qualia.Domain.Enums;

namespace Qualia.Domain.Entidades;

/// Disputa de un agente sobre una evaluación oficial. Mientras está Abierta no tiene resolución,
/// resolutor ni fecha de resolución, y la evaluación se excluye de los promedios oficiales (RN-11).
/// Solo puede haber una disputa Abierta por evaluación.
public class Disputa : IEntidadEmpresa
{
    public const int LongitudMinimaTexto = 10;
    public const int LongitudMaximaTexto = 2000;

    public Guid EmpresaId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EvaluacionId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public EstadoDisputa Estado { get; set; } = EstadoDisputa.Abierta;
    public string? Resolucion { get; set; }
    public Guid? ResueltaPorId { get; set; }
    public DateTime CreadaEn { get; set; }
    public DateTime? ResueltaEn { get; set; }
}
