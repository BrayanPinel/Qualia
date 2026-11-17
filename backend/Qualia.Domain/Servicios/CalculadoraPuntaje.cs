namespace Qualia.Domain.Servicios;

/// <summary>
/// Reglas de cálculo (RN-06 y 7.8).
/// para que el total, el cumplimiento y el fallo crítico se calculen igual en todo el sistema.
/// </summary>
public static class CalculadoraPuntaje
{
    /// <summary>Un criterio se cumple cuando su puntaje es mayor o igual al umbral.</summary>
    public static bool Cumple(decimal puntaje, decimal umbralCumplimiento) => puntaje >= umbralCumplimiento;

    /// <summary>falloCritico = esCritico AND puntaje &lt; umbral.</summary>
    public static bool EsFalloCritico(bool esCritico, decimal puntaje, decimal umbralCumplimiento) =>
        esCritico && puntaje < umbralCumplimiento;

    /// <summary>Suma de puntaje × peso / 100, redondeada a dos decimales al finalizar la suma.</summary>
    public static decimal Total(IEnumerable<(decimal Puntaje, decimal Peso)> criterios) =>
        Math.Round(criterios.Sum(c => c.Puntaje * c.Peso / 100m), 2, MidpointRounding.AwayFromZero);
}
