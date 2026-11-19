namespace Qualia.Application.Features.Suscripciones.Comun;

/// SuscripcionDto. Estado usa los valores de
/// EstadoSuscripcion: Prueba, Activa, EnMora, Vencida.
/// PeriodoActual es una ampliación aditiva y opcional: el periodo de
/// consumo del mes UTC en curso, o null si aún no existe.
public sealed record SuscripcionDto(
    Guid Id,
    Guid EmpresaId,
    Guid PlanId,
    string Estado,
    DateTime FechaVencimiento,
    bool PermiteExcedente,
    PeriodoConsumoDto? PeriodoActual = null);

/// Resumen del periodo de consumo (ampliación de SuscripcionDto).
public sealed record PeriodoConsumoDto(
    Guid Id,
    string Periodo,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal MinutosContratados);
