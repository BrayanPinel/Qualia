using FluentValidation;
using Qualia.Application.Features.Campanas.Dtos;
using Qualia.Domain.Entidades;

namespace Qualia.Application.Features.Campanas.Validadores;

// Límites del contrato: nombres de 3 a 100 caracteres; rúbricas de 1 a 30 criterios;
// pesos positivos; umbrales entre 0 y 100. La suma de 100 se exige al PUBLICAR, no al guardar el borrador.

public sealed class GuardarCampanaValidator : AbstractValidator<GuardarCampanaDto>
{
    public GuardarCampanaValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Activa)
            .NotNull().WithMessage("Indique si la campaña está activa.");
    }
}

public sealed class CrearRubricaValidator : AbstractValidator<CrearRubricaDto>
{
    public CrearRubricaValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Criterios)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Los criterios son obligatorios.")
            .Must(c => c!.Count is >= Rubrica.MinimoCriterios and <= Rubrica.MaximoCriterios)
                .WithMessage($"La rúbrica debe tener entre {Rubrica.MinimoCriterios} y {Rubrica.MaximoCriterios} criterios.")
            .Must(c => c!.All(x => x is not null)).WithMessage("Hay criterios vacíos.")
            .Must(c => Rubrica.NombresDuplicados(c!.Select(x => x.Nombre ?? string.Empty)).Count == 0)
                .WithMessage("Hay nombres de criterio duplicados dentro de la versión.");

        RuleForEach(x => x.Criterios).SetValidator(new CriterioEntradaValidator()).When(x => x.Criterios is not null);
    }
}

public sealed class CriterioEntradaValidator : AbstractValidator<CriterioEntradaDto>
{
    public CriterioEntradaValidator()
    {
        RuleFor(x => x.Nombre)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El nombre del criterio es obligatorio.")
            .Must(n => n!.Trim().Length is >= 3 and <= 100).WithMessage("El nombre del criterio debe tener entre 3 y 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La descripción del criterio es obligatoria.")
            .Must(d => d!.Trim().Length <= 2000).WithMessage("La descripción no puede superar 2,000 caracteres.");

        RuleFor(x => x.Peso)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El peso es obligatorio.")
            .GreaterThan(0).WithMessage("El peso debe ser positivo.")
            .LessThanOrEqualTo(100).WithMessage("El peso no puede ser mayor que 100.")
            .PrecisionScale(5, 2, ignoreTrailingZeros: true).WithMessage("El peso admite como máximo dos decimales.");

        RuleFor(x => x.EsCritico)
            .NotNull().WithMessage("Indique si el criterio es crítico.");

        RuleFor(x => x.UmbralCumplimiento)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El umbral de cumplimiento es obligatorio.")
            .InclusiveBetween(0, 100).WithMessage("El umbral de cumplimiento debe estar entre 0 y 100.")
            .PrecisionScale(5, 2, ignoreTrailingZeros: true).WithMessage("El umbral admite como máximo dos decimales.");
    }
}
