using Qualia.Application.Common.Models;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Disputas.Dtos;

// Entrada con propiedades anulables: un campo ausente se informa como error de validación.

/// CrearDisputaDto { motivo }.
public sealed class CrearDisputaDto
{
    public string? Motivo { get; set; }
}

public enum DecisionDisputa
{
    Mantener,
    Modificar,
}

/// ResolverDisputaDto { decision, resolucion, revisionEsperada, criterios? }.
/// Modificar exige la lista completa de ajustes (mismas reglas que el ajuste del supervisor); Mantener la omite.
public sealed class ResolverDisputaDto
{
    public DecisionDisputa? Decision { get; set; }
    public string? Resolucion { get; set; }
    public int? RevisionEsperada { get; set; }
    public List<AjusteCriterioDto>? Criterios { get; set; }
}

/// GET /api/empresas/{empresaId}/disputas: paginación y estado opcional.
public sealed class FiltroDisputasDto : PaginacionDto
{
    public EstadoDisputa? Estado { get; set; }
}

/// DisputaDto { id, empresaId, evaluacionId, llamadaId, estado, motivo, resolucion?, resueltaPorId?, creadaEn, resueltaEn? }.
/// llamadaId permite pedir la evaluación y la transcripción, que se consultan por llamada.
public sealed record DisputaDto(
    Guid Id,
    Guid EmpresaId,
    Guid EvaluacionId,
    Guid LlamadaId,
    EstadoDisputa Estado,
    string Motivo,
    string? Resolucion,
    Guid? ResueltaPorId,
    DateTime CreadaEn,
    DateTime? ResueltaEn)
{
    public static DisputaDto Desde(Disputa d, Guid llamadaId) =>
        new(d.Id, d.EmpresaId, d.EvaluacionId, llamadaId, d.Estado, d.Motivo, d.Resolucion, d.ResueltaPorId, d.CreadaEn, d.ResueltaEn);
}
