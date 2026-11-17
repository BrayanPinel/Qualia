using FluentValidation;
using Qualia.Application.Common.Interfaces;

namespace Qualia.Application.Features.Suscripciones.Consumo.ConsumoEvaluacion;

public sealed class ReservarConsumoValidator : AbstractValidator<ReservarConsumoRequest>
{
    public ReservarConsumoValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEmpty().WithMessage("La empresa es obligatoria.");

        RuleFor(x => x.LlamadaId)
            .NotEmpty().WithMessage("La llamada es obligatoria.");

        RuleFor(x => x.DuracionSeg)
            .GreaterThan(0m).WithMessage("La duración de la llamada debe ser mayor que cero.");

        RuleFor(x => x.FechaInicioUtc)
            .Must(f => !f.HasValue || f.Value.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha de inicio de evaluación debe expresarse en UTC.");
    }
}

public sealed class RegistrarUsoTecnicoValidator : AbstractValidator<RegistrarUsoTecnicoRequest>
{
    public RegistrarUsoTecnicoValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEmpty().WithMessage("La empresa es obligatoria.");

        RuleFor(x => x.LlamadaId)
            .NotEmpty().WithMessage("La llamada es obligatoria.");

        RuleFor(x => x.TokensTranscriptor)
            .GreaterThanOrEqualTo(0).WithMessage("Los tokens del transcriptor no pueden ser negativos.");

        RuleFor(x => x.TokensEntradaEvaluador)
            .GreaterThanOrEqualTo(0).WithMessage("Los tokens de entrada del evaluador no pueden ser negativos.");

        RuleFor(x => x.TokensSalidaEvaluador)
            .GreaterThanOrEqualTo(0).WithMessage("Los tokens de salida del evaluador no pueden ser negativos.");

        RuleFor(x => x.CostoEstimado)
            .GreaterThanOrEqualTo(0m).WithMessage("El costo estimado no puede ser negativo.");
    }
}
