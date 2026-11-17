using FluentValidation;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;

public sealed record CrearSuscripcionInicialRequest(
    Guid EmpresaId,
    Guid PlanId,
    DateTime FechaRegistroUtc);

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

/// <summary>
/// Implementación de <see cref="ICrearSuscripcionInicial"/> para el registro transaccional
/// de empresa (HU-01). No llama a <c>SaveChanges</c> de forma independiente para compartir
/// la unidad de trabajo del llamador.
/// </summary>
public sealed class CrearSuscripcionInicialService : ICrearSuscripcionInicial
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IValidator<CrearSuscripcionInicialRequest> _validador;

    public CrearSuscripcionInicialService(
        IRepositorioSuscripciones repositorio,
        IValidator<CrearSuscripcionInicialRequest> validador)
    {
        _repositorio = repositorio;
        _validador = validador;
    }

    public async Task<Resultado<SuscripcionDto>> CrearInicialAsync(
        Guid empresaId,
        Guid planId,
        DateTime fechaRegistroUtc,
        CancellationToken cancellationToken = default)
    {
        var request = new CrearSuscripcionInicialRequest(empresaId, planId, fechaRegistroUtc);
        var errorValidacion = await _validador.ValidarAsync(request, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        var plan = await _repositorio.ObtenerPlanAsync(planId, cancellationToken);
        if (plan is null || !plan.Activo)
        {
            return ErroresSuscripcion.PlanNoDisponible;
        }

        var existente = await _repositorio.ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
        if (existente is { EsVigente: true })
        {
            return ErroresSuscripcion.SuscripcionYaExiste;
        }

        var suscripcion = Suscripcion.CrearPrueba(empresaId, plan, fechaRegistroUtc);
        var mesInicial = PeriodoMensual.DesdeFecha(fechaRegistroUtc);
        var periodoInicial = PeriodoConsumo.Abrir(suscripcion, mesInicial, plan.MinutosIncluidos);

        _repositorio.Agregar(suscripcion);
        _repositorio.Agregar(periodoInicial);

        return suscripcion.ASuscripcionDto(periodoInicial);
    }
}
