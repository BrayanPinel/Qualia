namespace Qualia.Domain.Suscripciones;

/// Saldo de un periodo de consumo. Sean C los minutos contratados,
/// R la suma de reservados y F la suma de facturables:
/// Disponibles = max(C - R - F, 0)
/// Excedentes = max(F - C, 0) (excedente ya facturado, sin reservas)
public readonly record struct SaldoConsumo(
    decimal MinutosContratados,
    decimal MinutosReservados,
    decimal MinutosFacturables)
{
    public static SaldoConsumo Vacio { get; } = new(0m, 0m, 0m);

    public decimal MinutosDisponibles =>
        Math.Max(MinutosContratados - MinutosReservados - MinutosFacturables, 0m);

    public decimal MinutosExcedentes =>
        Math.Max(MinutosFacturables - MinutosContratados, 0m);

    /// Sin permiso de excedente una nueva reserva N exige R + F + N &lt;= C;
    /// con permiso se admite superar la cuota.
    public bool AdmiteReserva(decimal minutosNuevos, bool permiteExcedente)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutosNuevos);
        return permiteExcedente ||
               MinutosReservados + MinutosFacturables + minutosNuevos <= MinutosContratados;
    }
}
