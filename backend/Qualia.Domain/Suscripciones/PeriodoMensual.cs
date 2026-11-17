using System.Globalization;
using Qualia.Domain.Common;

namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Mes calendario UTC usado para consumo y facturación (fase1 §7.8):
/// desde el primer día a las 00:00 UTC (incluido) hasta el primer día del mes
/// siguiente (excluido). Su representación textual es <c>YYYY-MM</c>.
/// </summary>
public readonly record struct PeriodoMensual
{
    public const string Formato = "yyyy-MM";

    private PeriodoMensual(int anio, int mes)
    {
        Anio = anio;
        Mes = mes;
    }

    public int Anio { get; }

    public int Mes { get; }

    /// <summary>Primer instante del mes (incluido).</summary>
    public DateTime InicioUtc => new(Anio, Mes, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Primer instante del mes siguiente (excluido).</summary>
    public DateTime FinUtc => InicioUtc.AddMonths(1);

    public static PeriodoMensual Crear(int anio, int mes)
    {
        // Se excluye el año 9999 para que FinUtc siempre sea representable.
        ArgumentOutOfRangeException.ThrowIfLessThan(anio, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(anio, 9998);
        ArgumentOutOfRangeException.ThrowIfLessThan(mes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mes, 12);

        return new PeriodoMensual(anio, mes);
    }

    /// <summary>Mes calendario UTC que contiene el instante indicado.</summary>
    public static PeriodoMensual DesdeFecha(DateTime instanteUtc)
    {
        FechasUtc.Exigir(instanteUtc, nameof(instanteUtc));
        return Crear(instanteUtc.Year, instanteUtc.Month);
    }

    /// <summary>Interpreta exactamente el formato <c>YYYY-MM</c>.</summary>
    public static bool TryParse(string? texto, out PeriodoMensual periodo)
    {
        periodo = default;

        if (string.IsNullOrWhiteSpace(texto) ||
            texto.Length != Formato.Length ||
            !DateTime.TryParseExact(
                texto,
                Formato,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fecha) ||
            fecha.Year > 9998)
        {
            return false;
        }

        periodo = new PeriodoMensual(fecha.Year, fecha.Month);
        return true;
    }

    public PeriodoMensual Siguiente() => DesdeFecha(FinUtc);

    public bool Contiene(DateTime instanteUtc) =>
        FechasUtc.Exigir(instanteUtc, nameof(instanteUtc)) >= InicioUtc && instanteUtc < FinUtc;

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Anio:D4}-{Mes:D2}");
}
