namespace Qualia.Application.Features.Suscripciones.Comun;

/// PlanDto.
public sealed record PlanDto(
    Guid Id,
    string Nombre,
    decimal PrecioMensual,
    decimal MinutosIncluidos,
    int? LimiteSupervisores,
    int? LimiteRubricas,
    decimal PrecioExcedente,
    IReadOnlyList<string> Funciones);
