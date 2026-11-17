using FluentValidation;

namespace Qualia.Application.Features.Suscripciones.PagoSimulado;

/// <summary>PagoSimuladoDto : resultado "Aprobado" | "Rechazado".</summary>
public sealed record PagoSimuladoDto(string Resultado);

public sealed class PagoSimuladoValidator : AbstractValidator<PagoSimuladoDto>
{
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";

    public PagoSimuladoValidator()
    {
        RuleFor(x => x.Resultado)
            .NotEmpty().WithMessage("El resultado del pago simulado es obligatorio.")
            .Must(r => string.Equals(r?.Trim(), Aprobado, StringComparison.Ordinal) ||
                       string.Equals(r?.Trim(), Rechazado, StringComparison.Ordinal))
            .WithMessage("El resultado debe ser exactamente 'Aprobado' o 'Rechazado'.");
    }
}
