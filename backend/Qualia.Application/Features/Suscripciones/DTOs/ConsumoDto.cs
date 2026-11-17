namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>ConsumoDto (fase1 §7.7). <see cref="Periodo"/> con formato YYYY-MM.</summary>
public sealed record ConsumoDto(
    Guid EmpresaId,
    string Periodo,
    decimal MinutosContratados,
    decimal MinutosReservados,
    decimal MinutosFacturables,
    decimal MinutosDisponibles,
    decimal MinutosExcedentes);

/// <summary>Estado de reserva/facturación de una llamada individual.</summary>
public sealed record ConsumoLlamadaDto(
    Guid Id,
    Guid EmpresaId,
    Guid LlamadaId,
    Guid PeriodoId,
    decimal MinutosReservados,
    decimal MinutosFacturables,
    string Estado);
