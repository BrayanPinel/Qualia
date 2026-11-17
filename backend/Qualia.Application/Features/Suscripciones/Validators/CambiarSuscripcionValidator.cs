using FluentValidation;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;

public sealed class CambiarSuscripcionValidator : AbstractValidator<CambiarSuscripcionDto>
{
    public const int LongitudMinimaMotivo = 10;
    public const int LongitudMaximaMotivo = 2000;

    public CambiarSuscripcionValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("El plan es obligatorio.");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado de la suscripción es obligatorio.")
            .Must(estado => !string.IsNullOrWhiteSpace(estado) &&
                            Enum.TryParse<EstadoSuscripcion>(estado.Trim(), ignoreCase: false, out _))
            .WithMessage("El estado debe ser uno de: Prueba, Activa, EnMora o Vencida.");

        RuleFor(x => x.FechaVencimiento)
            .Must(f => f != default)
            .WithMessage("La fecha de vencimiento es obligatoria.")
            .Must(f => f.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha de vencimiento debe expresarse en UTC.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo del cambio es obligatorio.")
            .Must(m => !string.IsNullOrWhiteSpace(m) &&
                       m.Trim().Length is >= LongitudMinimaMotivo and <= LongitudMaximaMotivo)
            .WithMessage($"El motivo debe tener entre {LongitudMinimaMotivo} y {LongitudMaximaMotivo} caracteres.");
    }
}

public sealed class CambiarSuscripcionCommandValidator : AbstractValidator<CambiarSuscripcionCommand>
{
    public CambiarSuscripcionCommandValidator(IValidator<CambiarSuscripcionDto> datosValidator)
    {
        RuleFor(x => x.EmpresaId)
            .NotEmpty().WithMessage("La empresa objetivo es obligatoria.");

        RuleFor(x => x.AdministradorId)
            .NotEmpty().WithMessage("El administrador de plataforma es obligatorio.");

        RuleFor(x => x.Datos)
            .NotNull().WithMessage("Los datos de la suscripción son obligatorios.")
            .SetValidator(datosValidator);
    }
}
