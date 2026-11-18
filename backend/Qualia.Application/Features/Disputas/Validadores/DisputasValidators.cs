using FluentValidation;
using Qualia.Application.Common.Models;
using Qualia.Application.Features.Disputas.Dtos;
using Qualia.Domain.Entidades;

namespace Qualia.Application.Features.Disputas.Validadores;

// Motivos y justificaciones de 10 a 2.000 caracteres (contrato). El detalle de los criterios de
// "Modificar" lo valida el ajuste del supervisor, que es el mismo para revisiones y disputas.

public sealed class CrearDisputaValidator : AbstractValidator<CrearDisputaDto>
{
    public CrearDisputaValidator()
    {
        RuleFor(x => x.Motivo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El motivo es obligatorio.")
            .Must(m => m!.Trim().Length is >= Disputa.LongitudMinimaTexto and <= Disputa.LongitudMaximaTexto)
                .WithMessage($"El motivo debe tener entre {Disputa.LongitudMinimaTexto} y {Disputa.LongitudMaximaTexto} caracteres.");
    }
}

public sealed class ResolverDisputaValidator : AbstractValidator<ResolverDisputaDto>
{
    public ResolverDisputaValidator()
    {
        RuleFor(x => x.Decision)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("La decisión es obligatoria (Mantener o Modificar).")
            .IsInEnum().WithMessage("La decisión debe ser Mantener o Modificar.");

        RuleFor(x => x.Resolucion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La resolución es obligatoria.")
            .Must(r => r!.Trim().Length is >= Disputa.LongitudMinimaTexto and <= Disputa.LongitudMaximaTexto)
                .WithMessage($"La resolución debe tener entre {Disputa.LongitudMinimaTexto} y {Disputa.LongitudMaximaTexto} caracteres.");

        RuleFor(x => x.RevisionEsperada)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("La revisión esperada es obligatoria.")
            .GreaterThan(0).WithMessage("La revisión esperada debe ser mayor que cero.");

        RuleFor(x => x.Criterios)
            .NotEmpty().When(x => x.Decision == DecisionDisputa.Modificar)
                .WithMessage("Modificar exige la lista completa de criterios ajustados.")
            .Empty().When(x => x.Decision == DecisionDisputa.Mantener)
                .WithMessage("Mantener no admite criterios: las notas no cambian.");
    }
}

public sealed class FiltroDisputasValidator : AbstractValidator<FiltroDisputasDto>
{
    public FiltroDisputasValidator()
    {
        Include(new PaginacionDtoValidator());
        RuleFor(x => x.Estado).IsInEnum().WithMessage("El estado debe ser Abierta o Resuelta.");
    }
}
