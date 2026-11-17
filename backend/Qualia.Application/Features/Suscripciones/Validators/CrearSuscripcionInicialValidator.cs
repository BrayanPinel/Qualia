using FluentValidation;

namespace Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;

public sealed class CrearSuscripcionInicialValidator : AbstractValidator<CrearSuscripcionInicialRequest>
{
    public CrearSuscripcionInicialValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEmpty().WithMessage("La empresa es obligatoria.");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("El plan es obligatorio.");

        RuleFor(x => x.FechaRegistroUtc)
            .Must(f => f.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha de registro debe expresarse en UTC.");
    }
}
