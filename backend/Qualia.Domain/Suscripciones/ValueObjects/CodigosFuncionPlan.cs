namespace Qualia.Domain.Suscripciones;

/// <summary>
/// Códigos de Funcion_plan.codigo_funcion (varchar(50)).
/// Contrato compartido: los roles B y D deben usar estas constantes al consultar
/// IConsultaPlanService.TieneFuncionAsync en lugar de literales.
/// </summary>
public static class CodigosFuncionPlan
{
    /// <summary>Evaluación asistida de llamadas (todos los planes).</summary>
    public const string Evaluacion = "EVALUACION";

    /// <summary>Scorecard individual básico (todos los planes).</summary>
    public const string ScorecardBasico = "SCORECARD_BASICO";

    /// <summary>Tendencia respecto al mes anterior (Pro y Enterprise).</summary>
    public const string Tendencia = "TENDENCIA";

    /// <summary>Comparación por campaña / promedio de campaña (Pro y Enterprise).</summary>
    public const string ComparacionCampanas = "COMPARACION_CAMPANAS";

    /// <summary>Disputas de evaluaciones por parte del agente (Pro y Enterprise).</summary>
    public const string Disputas = "DISPUTAS";

    public static IReadOnlyList<string> Todos { get; } =
    [
        Evaluacion,
        ScorecardBasico,
        Tendencia,
        ComparacionCampanas,
        Disputas,
    ];

    public static bool EsValido(string? codigoFuncion) =>
        !string.IsNullOrWhiteSpace(codigoFuncion) &&
        Todos.Contains(codigoFuncion.Trim(), StringComparer.OrdinalIgnoreCase);
}
