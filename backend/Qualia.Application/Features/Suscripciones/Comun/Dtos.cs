namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>PlanDto (fase1 §7.7).</summary>
public sealed record PlanDto(
    Guid Id,
    string Nombre,
    decimal PrecioMensual,
    decimal MinutosIncluidos,
    int? LimiteSupervisores,
    int? LimiteRubricas,
    decimal PrecioExcedente,
    IReadOnlyList<string> Funciones);

/// <summary>
/// SuscripcionDto (fase1 §7.7). <see cref="Estado"/> usa los valores de
/// EstadoSuscripcion: Prueba, Activa, EnMora, Vencida.
/// <see cref="PeriodoActual"/> es una ampliación aditiva y opcional: el periodo de
/// consumo del mes UTC en curso, o <c>null</c> si aún no existe.
/// </summary>
public sealed record SuscripcionDto(
    Guid Id,
    Guid EmpresaId,
    Guid PlanId,
    string Estado,
    DateTime FechaVencimiento,
    bool PermiteExcedente,
    PeriodoConsumoDto? PeriodoActual);

/// <summary>Resumen del periodo de consumo (ampliación de SuscripcionDto).</summary>
public sealed record PeriodoConsumoDto(
    Guid Id,
    string Periodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal MinutosContratados);

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

/// <summary>EmpresaComercialDto (fase1 §7.7) para administración global por PlatformAdmin.</summary>
public sealed record EmpresaComercialDto(
    Guid EmpresaId,
    string Codigo,
    string Nombre,
    string Estado,
    SuscripcionDto? Suscripcion,
    ConsumoDto? Consumo);
