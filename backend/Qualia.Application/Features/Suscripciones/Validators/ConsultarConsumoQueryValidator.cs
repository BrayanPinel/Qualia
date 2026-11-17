using FluentValidation;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

public sealed class ConsultarConsumoQueryValidator : AbstractValidator<ConsultarConsumoQuery>
{
    public ConsultarConsumoQueryValidator()
    {
        RuleFor(x => x.Periodo)
            .Must(p => string.IsNullOrWhiteSpace(p) || PeriodoMensual.TryParse(p.Trim(), out _))
            .WithMessage("El periodo debe tener formato YYYY-MM válido.");
    }
}
