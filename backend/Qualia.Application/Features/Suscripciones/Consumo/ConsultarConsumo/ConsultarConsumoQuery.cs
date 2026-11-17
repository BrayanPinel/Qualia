using FluentValidation;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// <summary>Parámetro de consulta <c>GET /api/consumo?periodo=YYYY-MM</c> (fase1 §7.2).</summary>
public sealed record ConsultarConsumoQuery(string? Periodo);

public sealed class ConsultarConsumoQueryValidator : AbstractValidator<ConsultarConsumoQuery>
{
    public ConsultarConsumoQueryValidator()
    {
        RuleFor(x => x.Periodo)
            .Must(p => string.IsNullOrWhiteSpace(p) || PeriodoMensual.TryParse(p.Trim(), out _))
            .WithMessage("El periodo debe tener formato YYYY-MM válido.");
    }
}
