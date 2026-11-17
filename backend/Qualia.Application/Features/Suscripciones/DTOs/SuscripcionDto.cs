namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>
/// SuscripcionDto. <see cref="Estado"/> usa los valores de
/// EstadoSuscripcion: Prueba, Activa, EnMora, Vencida.
/// <see cref="PeriodoActual"/> es una ampliación aditiva y opcional: el periodo de
/// consumo del mes UTC en curso, o null si aún no existe.
/// </summary>
public sealed record SuscripcionDto(
    Guid Id,
    Guid EmpresaId,
    Guid PlanId,
    string Estado,
    DateTime FechaVencimiento,
    bool PermiteExcedente,
    PeriodoConsumoDto? PeriodoActual = null);

/// <summary>Resumen del periodo de consumo (ampliación de SuscripcionDto).</summary>
public sealed record PeriodoConsumoDto(
    Guid Id,
    string Periodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal MinutosContratados);
