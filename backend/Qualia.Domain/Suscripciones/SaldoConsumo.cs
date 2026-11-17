namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Saldo de un periodo de consumo (fase1 §7.8). Sean C los minutos contratados,
/// R la suma de reservados y F la suma de facturables:
/// <list type="bullet">
/// <item><c>Disponibles = max(C - R - F, 0)</c></item>
/// <item><c>Excedentes = max(F - C, 0)</c> (excedente ya facturado, sin reservas)</item>
/// </list>
/// </summary>
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

    /// <summary>
    /// Sin permiso de excedente una nueva reserva N exige <c>R + F + N &lt;= C</c>;
    /// con permiso se admite superar la cuota.
    /// </summary>
    public bool AdmiteReserva(decimal minutosNuevos, bool permiteExcedente)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minutosNuevos);
        return permiteExcedente ||
               MinutosReservados + MinutosFacturables + minutosNuevos <= MinutosContratados;
    }
}
