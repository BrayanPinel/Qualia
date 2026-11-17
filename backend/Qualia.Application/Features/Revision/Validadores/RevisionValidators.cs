using FluentValidation;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Revision.Dtos;
using Qualia.Domain.Entidades;

namespace Qualia.Application.Features.Revision.Validadores;

// Forma de las peticiones. Las reglas que dependen de datos (criterios de la rúbrica, segmentos
// de la llamada, revisión vigente) se comprueban en el caso de uso.

public sealed class ConfirmarEvaluacionValidator : AbstractValidator<ConfirmarEvaluacionDto>
{
    public ConfirmarEvaluacionValidator()
    {
        RuleFor(x => x.RevisionEsperada)
            .NotNull().WithMessage("La revisión esperada es obligatoria.")
            .GreaterThan(0).WithMessage("La revisión esperada debe ser mayor que cero.");
    }
}

public sealed class AjustarEvaluacionValidator : AbstractValidator<AjustarEvaluacionDto>
{
    public AjustarEvaluacionValidator()
    {
        RuleFor(x => x.RevisionEsperada)
            .NotNull().WithMessage("La revisión esperada es obligatoria.")
            .GreaterThan(0).WithMessage("La revisión esperada debe ser mayor que cero.");

        RuleFor(x => x.Motivo).SetValidator(new TextoJustificadoValidator("El motivo", femenino: false));

        RuleFor(x => x.Criterios)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("El ajuste debe incluir los criterios de la rúbrica.")
            .Must(c => c!.Count <= Rubrica.MaximoCriterios).WithMessage($"Un ajuste no puede tener más de {Rubrica.MaximoCriterios} criterios.")
            .Must(c => c!.All(x => x is not null)).WithMessage("Hay criterios vacíos.")
            .Must(c => c!.Where(x => x.CriterioId is not null).GroupBy(x => x.CriterioId).All(g => g.Count() == 1))
                .WithMessage("Cada criterio debe aparecer exactamente una vez.");

        RuleForEach(x => x.Criterios).SetValidator(new AjusteCriterioValidator()).When(x => x.Criterios is not null);
    }
}

public sealed class AjusteCriterioValidator : AbstractValidator<AjusteCriterioDto>
{
    public AjusteCriterioValidator()
    {
        RuleFor(x => x.CriterioId)
            .NotNull().WithMessage("El criterio es obligatorio.")
            .NotEqual(Guid.Empty).WithMessage("El identificador de criterio no es válido.");

        RuleFor(x => x.Puntaje)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("El puntaje es obligatorio.")
            .InclusiveBetween(0, 100).WithMessage("El puntaje debe estar entre 0 y 100.")
            .PrecisionScale(5, 2, ignoreTrailingZeros: true).WithMessage("El puntaje admite como máximo dos decimales.");

        RuleFor(x => x.Justificacion).SetValidator(new TextoJustificadoValidator("La justificación", femenino: true));

        RuleFor(x => x.Evidencias)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Indique las evidencias; use una lista vacía si no hay evidencia.")
            .Must(e => e!.All(x => x is not null)).WithMessage("Hay evidencias vacías.")
            .Must(e => e!.Where(x => x.SegmentoId is not null).GroupBy(x => x.SegmentoId).All(g => g.Count() == 1))
                .WithMessage("Use una sola cita por segmento en cada criterio.");

        RuleForEach(x => x.Evidencias).ChildRules(e =>
        {
            e.RuleFor(x => x.SegmentoId)
                .NotNull().WithMessage("El segmento es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de segmento no es válido.");
            e.RuleFor(x => x.CitaTextual)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("La cita textual es obligatoria.")
                .MaximumLength(ConstructorEvaluacion.LongitudMaximaCita)
                    .WithMessage($"La cita no puede superar {ConstructorEvaluacion.LongitudMaximaCita} caracteres.");
        }).When(x => x.Evidencias is not null);
    }
}

/// <summary>
/// Motivos y justificaciones: de 10 a 2,000 caracteres.
/// Interno para que AddValidatorsFromAssembly no lo registre (requiere parámetros).
/// </summary>
internal sealed class TextoJustificadoValidator : AbstractValidator<string?>
{
    public TextoJustificadoValidator(string campo, bool femenino)
    {
        RuleFor(x => x)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage($"{campo} es {(femenino ? "obligatoria" : "obligatorio")}.")
            .Must(t => t!.Trim().Length is >= 10 and <= 2000).WithMessage($"{campo} debe tener entre 10 y 2,000 caracteres.");
    }
}
