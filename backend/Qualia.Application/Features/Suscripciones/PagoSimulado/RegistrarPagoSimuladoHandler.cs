using FluentValidation;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;
using Qualia.Application.Common.Seguridad;

namespace Qualia.Application.Features.Suscripciones.PagoSimulado;

/// <summary>
/// Caso de uso POST /api/suscripcion/pago-simulado.
/// </summary>
public sealed class RegistrarPagoSimuladoHandler
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IContextoEmpresa _contextoEmpresa;
    private readonly IValidator<PagoSimuladoDto> _validador;
    private readonly TimeProvider _timeProvider;
    private readonly IRegistroAuditoria? _registroAuditoria;

    public RegistrarPagoSimuladoHandler(
        IRepositorioSuscripciones repositorio,
        IContextoEmpresa contextoEmpresa,
        IValidator<PagoSimuladoDto> validador,
        TimeProvider timeProvider,
        IRegistroAuditoria? registroAuditoria = null)
    {
        _repositorio = repositorio;
        _contextoEmpresa = contextoEmpresa;
        _validador = validador;
        _timeProvider = timeProvider;
        _registroAuditoria = registroAuditoria;
    }

    public async Task<Resultado<SuscripcionDto>> EjecutarAsync(
        PagoSimuladoDto dto,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validador.ValidarAsync(dto, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        if (_contextoEmpresa.EmpresaIdActual is not { } empresaId || empresaId == Guid.Empty)
        {
            return ErroresSuscripcion.EmpresaNoIdentificada;
        }

        await using var transaccion = await _repositorio.IniciarTransaccionAsync(cancellationToken);

        var suscripcion = await _repositorio.BloquearSuscripcionActualAsync(empresaId, cancellationToken);
        if (suscripcion is null)
        {
            return ErroresSuscripcion.SuscripcionNoEncontrada;
        }

        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var resultadoNormalizado = dto.Resultado.Trim();
        var estadoAnterior = new
        {
            Estado = suscripcion.Estado.ToString(),
            suscripcion.FechaVencimiento,
        };

        if (string.Equals(resultadoNormalizado, PagoSimuladoValidator.Rechazado, StringComparison.Ordinal))
        {
            if (suscripcion.RegistrarPagoRechazado(ahoraUtc))
            {
                if (_registroAuditoria is not null)
                {
                    await _registroAuditoria.RegistrarAsync(
                        empresaId,
                        usuarioId: null,
                        "PAGO_SIMULADO_RECHAZADO",
                        "Suscripcion",
                        suscripcion.Id,
                        estadoAnterior,
                        new { Estado = suscripcion.Estado.ToString(), suscripcion.FechaVencimiento },
                        cancellationToken);
                }

                await _repositorio.GuardarCambiosAsync(cancellationToken);
                await transaccion.ConfirmarAsync(cancellationToken);
            }

            return ErroresSuscripcion.PagoRechazado;
        }

        var plan = suscripcion.Plan ?? await _repositorio.ObtenerPlanAsync(suscripcion.PlanId, cancellationToken);
        if (plan is null)
        {
            return ErroresSuscripcion.PlanNoDisponible;
        }

        suscripcion.RegistrarPagoAprobado(ahoraUtc);

        var mesActual = PeriodoMensual.DesdeFecha(ahoraUtc);
        var periodoActual = await _repositorio.ObtenerPeriodoConBloqueoAsync(empresaId, mesActual, cancellationToken);
        if (periodoActual is null)
        {
            periodoActual = PeriodoConsumo.Abrir(suscripcion, mesActual, plan.MinutosIncluidos);
            _repositorio.Agregar(periodoActual);
        }

        if (_registroAuditoria is not null)
        {
            await _registroAuditoria.RegistrarAsync(
                empresaId,
                usuarioId: null,
                "PAGO_SIMULADO_APROBADO",
                "Suscripcion",
                suscripcion.Id,
                estadoAnterior,
                new { Estado = suscripcion.Estado.ToString(), suscripcion.FechaVencimiento },
                cancellationToken);
        }

        await _repositorio.GuardarCambiosAsync(cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return suscripcion.ASuscripcionDto(periodoActual);
    }
}
