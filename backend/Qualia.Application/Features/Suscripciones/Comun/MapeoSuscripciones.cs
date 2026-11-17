using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>Conversión de entidades del módulo a DTOs de salida.</summary>
public static class MapeoSuscripciones
{
    public static PlanDto APlanDto(this Plan plan) =>
        new(
            plan.Id,
            plan.Nombre,
            plan.PrecioMensual,
            plan.MinutosIncluidos,
            plan.LimiteSupervisores,
            plan.LimiteRubricas,
            plan.PrecioExcedente,
            plan.Funciones
                .Select(f => f.CodigoFuncion)
                .Order(StringComparer.Ordinal)
                .ToArray());

    public static SuscripcionDto ASuscripcionDto(this Suscripcion suscripcion, PeriodoConsumo? periodoActual) =>
        new(
            suscripcion.Id,
            suscripcion.EmpresaId,
            suscripcion.PlanId,
            suscripcion.Estado.ToString(),
            suscripcion.FechaVencimiento,
            suscripcion.PermiteExcedente,
            periodoActual?.APeriodoConsumoDto());

    public static PeriodoConsumoDto APeriodoConsumoDto(this PeriodoConsumo periodo) =>
        new(
            periodo.Id,
            periodo.Mes.ToString(),
            periodo.FechaInicio,
            periodo.FechaFin,
            periodo.MinutosContratados);

    public static ConsumoDto AConsumoDto(this SaldoConsumo saldo, Guid empresaId, PeriodoMensual periodo) =>
        new(
            empresaId,
            periodo.ToString(),
            saldo.MinutosContratados,
            saldo.MinutosReservados,
            saldo.MinutosFacturables,
            saldo.MinutosDisponibles,
            saldo.MinutosExcedentes);

    public static ConsumoLlamadaDto AConsumoLlamadaDto(this ConsumoLlamada consumo) =>
        new(
            consumo.Id,
            consumo.EmpresaId,
            consumo.LlamadaId,
            consumo.PeriodoId,
            consumo.MinutosReservados,
            consumo.MinutosFacturables,
            consumo.Estado.ToString());
}
