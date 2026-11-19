namespace Qualia.Application.Features.Suscripciones.Comun;

/// ConsumoDto. Periodo con formato YYYY-MM.
public sealed record ConsumoDto(
    Guid EmpresaId,
    string Periodo,
    decimal MinutosContratados,
    decimal MinutosReservados,
    decimal MinutosFacturables,
    decimal MinutosDisponibles,
    decimal MinutosExcedentes);

/// Estado de reserva/facturación de una llamada individual.
public sealed record ConsumoLlamadaDto(
    Guid Id,
    Guid EmpresaId,
    Guid LlamadaId,
    Guid PeriodoId,
    decimal MinutosReservados,
    decimal MinutosFacturables,
    string Estado);
