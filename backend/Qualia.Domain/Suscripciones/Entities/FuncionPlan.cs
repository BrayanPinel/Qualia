namespace Qualia.Domain.Suscripciones;

/// Función habilitada por un plan (tabla global funcion_plan,
/// PK compuesta (plan_id, codigo_funcion)). Ver CodigosFuncionPlan.
public sealed class FuncionPlan
{
    public const int LongitudMaximaCodigo = 50;

    // Constructor para EF Core.
    private FuncionPlan()
    {
    }

    internal FuncionPlan(Guid planId, string codigoFuncion)
    {
        if (planId == Guid.Empty)
        {
            throw new ArgumentException("El plan es obligatorio.", nameof(planId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(codigoFuncion);

        var codigo = codigoFuncion.Trim().ToUpperInvariant();
        if (codigo.Length > LongitudMaximaCodigo)
        {
            throw new ArgumentException(
                $"El código de función admite como máximo {LongitudMaximaCodigo} caracteres.",
                nameof(codigoFuncion));
        }

        PlanId = planId;
        CodigoFuncion = codigo;
    }

    public Guid PlanId { get; private set; }

    public string CodigoFuncion { get; private set; } = string.Empty;
}
