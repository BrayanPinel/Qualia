using Qualia.Application.Common.Models;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Disputas.Dtos;

// Entrada con propiedades anulables: un campo ausente se informa como error de validación.

/// <summary>CrearDisputaDto { motivo }.</summary>
public sealed class CrearDisputaDto
{
    public string? Motivo { get; set; }
}

public enum DecisionDisputa
{
    Mantener,
    Modificar,
}

/// <summary>
/// ResolverDisputaDto { decision, resolucion, revisionEsperada, criterios? }.
/// Modificar exige la lista completa de ajustes (mismas reglas que el ajuste del supervisor); Mantener la omite.
/// </summary>
public sealed class ResolverDisputaDto
{
    public DecisionDisputa? Decision { get; set; }
    public string? Resolucion { get; set; }
    public int? RevisionEsperada { get; set; }
    public List<AjusteCriterioDto>? Criterios { get; set; }
}

/// <summary>GET /api/disputas: paginación y estado opcional.</summary>
public sealed class FiltroDisputasDto : PaginacionDto
{
    public EstadoDisputa? Estado { get; set; }
}

/// <summary>DisputaDto { id, empresaId, evaluacionId, estado, motivo, resolucion?, resueltaPorId?, creadaEn, resueltaEn? }.</summary>
public sealed record DisputaDto(
    Guid Id,
    Guid EmpresaId,
    Guid EvaluacionId,
    EstadoDisputa Estado,
    string Motivo,
    string? Resolucion,
    Guid? ResueltaPorId,
    DateTime CreadaEn,
    DateTime? ResueltaEn)
{
    public static DisputaDto Desde(Disputa d) =>
        new(d.Id, d.EmpresaId, d.EvaluacionId, d.Estado, d.Motivo, d.Resolucion, d.ResueltaPorId, d.CreadaEn, d.ResueltaEn);
}
