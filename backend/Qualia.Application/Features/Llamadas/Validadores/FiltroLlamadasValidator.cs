using FluentValidation;
using Qualia.Application.Features.Llamadas.Dtos;
using Qualia.Domain.Enums;

namespace Qualia.Application.Features.Llamadas.Validadores;

public sealed class FiltroLlamadasValidator : AbstractValidator<FiltroLlamadasDto>
{
    public FiltroLlamadasValidator()
    {
        RuleFor(x => x.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");

        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100.");

        RuleFor(x => x.Estado)
            .Must(e => e is null || Enum.TryParse<EstadoLlamada>(e, ignoreCase: false, out _) && !int.TryParse(e, out _))
            .WithMessage($"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<EstadoLlamada>())}.");

        RuleFor(x => x.CampanaId)
            .NotEqual(Guid.Empty).When(x => x.CampanaId.HasValue).WithMessage("El identificador de campaña no es válido.");

        RuleFor(x => x.AgenteId)
            .NotEqual(Guid.Empty).When(x => x.AgenteId.HasValue).WithMessage("El identificador de agente no es válido.");
    }
}
