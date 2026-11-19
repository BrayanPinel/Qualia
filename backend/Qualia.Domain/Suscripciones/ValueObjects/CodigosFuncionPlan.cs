namespace Qualia.Domain.Suscripciones;

/// Códigos de Funcion_plan.codigo_funcion (varchar(50)).
/// Contrato compartido: los roles B y D deben usar estas constantes al consultar
/// IConsultaPlanService.TieneFuncionAsync en lugar de literales.
public static class CodigosFuncionPlan
{
    /// Evaluación asistida de llamadas (todos los planes).
    public const string Evaluacion = "EVALUACION";

    /// Scorecard individual básico (todos los planes).
    public const string ScorecardBasico = "SCORECARD_BASICO";

    /// Tendencia respecto al mes anterior (Pro y Enterprise).
    public const string Tendencia = "TENDENCIA";

    /// Comparación por campaña / promedio de campaña (Pro y Enterprise).
    public const string ComparacionCampanas = "COMPARACION_CAMPANAS";

    /// Disputas de evaluaciones por parte del agente (Pro y Enterprise).
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
