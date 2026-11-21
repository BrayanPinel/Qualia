using FluentValidation;

namespace Qualia.Application.Features.Llamadas.Validadores;

public sealed class CargarLlamadaValidator : AbstractValidator<CargarLlamadaSolicitud>
{
    public CargarLlamadaValidator(TimeProvider reloj)
    {
        RuleFor(x => x.CampanaId)
            .NotEqual(Guid.Empty).WithMessage("La campaña es obligatoria.");

        RuleFor(x => x.AgenteId)
            .NotEqual(Guid.Empty).WithMessage("El agente es obligatorio.");

        RuleFor(x => x.NombreArchivo)
            .Must(n => CargarLlamada.ExtensionesPermitidas.Contains(Path.GetExtension(n ?? string.Empty).ToLowerInvariant()))
            .OverridePropertyName("audio")
            .WithMessage($"Formato no admitido. Formatos permitidos: {string.Join(", ", CargarLlamada.ExtensionesPermitidas)}.");

        RuleFor(x => x.FechaLlamada)
            .Must(f => f is null || f.Value.ToUniversalTime() <= reloj.GetUtcNow().UtcDateTime.AddMinutes(5))
            .WithMessage("La fecha de la llamada no puede estar en el futuro.");
    }
}
