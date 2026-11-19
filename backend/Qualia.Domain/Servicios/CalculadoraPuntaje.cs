namespace Qualia.Domain.Servicios;

/// Reglas de cálculo (RN-06 y 7.8).
/// para que el total, el cumplimiento y el fallo crítico se calculen igual en todo el sistema.
public static class CalculadoraPuntaje
{
    /// Un criterio se cumple cuando su puntaje es mayor o igual al umbral.
    public static bool Cumple(decimal puntaje, decimal umbralCumplimiento) => puntaje >= umbralCumplimiento;

    /// falloCritico = esCritico AND puntaje &lt; umbral.
    public static bool EsFalloCritico(bool esCritico, decimal puntaje, decimal umbralCumplimiento) =>
        esCritico && puntaje < umbralCumplimiento;

    /// Suma de puntaje × peso / 100, redondeada a dos decimales al finalizar la suma.
    public static decimal Total(IEnumerable<(decimal Puntaje, decimal Peso)> criterios) =>
        Math.Round(criterios.Sum(c => c.Puntaje * c.Peso / 100m), 2, MidpointRounding.AwayFromZero);
}
