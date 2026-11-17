namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Plan comercial (tabla global plan, sin empresa_id).
/// Los límites null significan "sin límite".
/// </summary>
public sealed class Plan
{
    public const int LongitudMaximaNombre = 50;

    private readonly List<FuncionPlan> _funciones = [];

    // Constructor para EF Core.
    private Plan()
    {
    }

    public Plan(
        Guid id,
        string nombre,
        decimal precioMensual,
        decimal minutosIncluidos,
        decimal precioExcedente,
        int? limiteSupervisores,
        int? limiteRubricas,
        int retencionDias,
        bool activo = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del plan es obligatorio.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (nombre.Trim().Length > LongitudMaximaNombre)
        {
            throw new ArgumentException(
                $"El nombre del plan admite como máximo {LongitudMaximaNombre} caracteres.",
                nameof(nombre));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(precioMensual);
        ArgumentOutOfRangeException.ThrowIfNegative(minutosIncluidos);
        ArgumentOutOfRangeException.ThrowIfNegative(precioExcedente);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retencionDias);
        if (limiteSupervisores is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limiteSupervisores));
        }

        if (limiteRubricas is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limiteRubricas));
        }

        Id = id;
        Nombre = nombre.Trim();
        PrecioMensual = precioMensual;
        MinutosIncluidos = minutosIncluidos;
        PrecioExcedente = precioExcedente;
        LimiteSupervisores = limiteSupervisores;
        LimiteRubricas = limiteRubricas;
        RetencionDias = retencionDias;
        Activo = activo;
    }

    public Guid Id { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public decimal PrecioMensual { get; private set; }

    public decimal MinutosIncluidos { get; private set; }

    public decimal PrecioExcedente { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>null = sin límite de supervisores.</summary>
    public int? LimiteSupervisores { get; private set; }

    /// <summary>null = sin límite de rúbricas activas.</summary>
    public int? LimiteRubricas { get; private set; }

    /// <summary>Parámetro previsto para la futura política de retención (365 en el MVP).</summary>
    public int RetencionDias { get; private set; }

    public IReadOnlyCollection<FuncionPlan> Funciones => _funciones;

    public bool TieneFuncion(string codigoFuncion)
    {
        if (string.IsNullOrWhiteSpace(codigoFuncion))
        {
            return false;
        }

        var normalizado = codigoFuncion.Trim();
        return _funciones.Any(f => string.Equals(f.CodigoFuncion, normalizado, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Agrega la función si aún no existe. Devuelve la función creada
    /// o null si el plan ya la tenía (operación idempotente).
    /// </summary>
    public FuncionPlan? AgregarFuncion(string codigoFuncion)
    {
        var funcion = new FuncionPlan(Id, codigoFuncion);
        if (TieneFuncion(funcion.CodigoFuncion))
        {
            return null;
        }

        _funciones.Add(funcion);
        return funcion;
    }

    /// <summary>¿El plan admite <paramref name="totalResultante"/> supervisores activos?</summary>
    public bool AdmiteSupervisores(int totalResultante) =>
        LimiteSupervisores is null || totalResultante <= LimiteSupervisores.Value;

    /// <summary>¿El plan admite <paramref name="totalResultante"/> rúbricas activas?</summary>
    public bool AdmiteRubricasActivas(int totalResultante) =>
        LimiteRubricas is null || totalResultante <= LimiteRubricas.Value;
}
