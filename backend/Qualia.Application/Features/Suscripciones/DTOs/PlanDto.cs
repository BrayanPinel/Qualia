namespace Qualia.Application.Features.Suscripciones.Comun;

/// <summary>PlanDto (fase1 §7.7).</summary>
public sealed record PlanDto(
    Guid Id,
    string Nombre,
    decimal PrecioMensual,
    decimal MinutosIncluidos,
    int? LimiteSupervisores,
    int? LimiteRubricas,
    decimal PrecioExcedente,
    IReadOnlyList<string> Funciones);
