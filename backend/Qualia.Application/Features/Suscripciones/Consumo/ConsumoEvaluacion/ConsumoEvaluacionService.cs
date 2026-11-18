using FluentValidation;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Consumo.ConsumoEvaluacion;

/// <summary>
/// Implementación del contrato compartido <see cref="IConsumoEvaluacion"/> (RN-13).
/// Gestiona reserva atómica, uso técnico por intento, facturación y liberación idempotentes.
/// </summary>
public sealed class ConsumoEvaluacionService : IConsumoEvaluacion
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IValidator<ReservarConsumoRequest> _validadorReserva;
    private readonly IValidator<RegistrarUsoTecnicoRequest> _validadorUsoTecnico;
    private readonly TimeProvider _timeProvider;

    public ConsumoEvaluacionService(
        IRepositorioSuscripciones repositorio,
        IValidator<ReservarConsumoRequest> validadorReserva,
        IValidator<RegistrarUsoTecnicoRequest> validadorUsoTecnico,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _validadorReserva = validadorReserva;
        _validadorUsoTecnico = validadorUsoTecnico;
        _timeProvider = timeProvider;
    }

    public async Task<Resultado<ConsumoLlamadaDto>> ReservarAsync(
        ReservarConsumoRequest request,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validadorReserva.ValidarAsync(request, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        await using var transaccion = await _repositorio.IniciarTransaccionAsync(cancellationToken);

        var suscripcion = await _repositorio.BloquearSuscripcionActualAsync(request.EmpresaId, cancellationToken);
        if (suscripcion is null)
        {
            return ErroresSuscripcion.SuscripcionNoEncontrada;
        }

        // Idempotencia: si la llamada ya tiene registro de consumo, no duplica la reserva (RN-13).
        // Un consumo liberado (llamada perdida) se reactiva en el mismo registro tras validar la cuota.
        var existente = await _repositorio.ObtenerConsumoLlamadaAsync(
            request.EmpresaId,
            request.LlamadaId,
            cancellationToken);

        if (existente is not null && existente.Estado != EstadoConsumo.Liberado)
        {
            await transaccion.ConfirmarAsync(cancellationToken);
            return existente.AConsumoLlamadaDto();
        }

        var fechaInicioUtc = request.FechaInicioUtc ?? _timeProvider.GetUtcNow().UtcDateTime;
        if (!suscripcion.PermiteNuevasEvaluaciones(fechaInicioUtc))
        {
            return ErroresSuscripcion.SuscripcionNoActiva;
        }

        var plan = suscripcion.Plan ?? await _repositorio.ObtenerPlanAsync(suscripcion.PlanId, cancellationToken);
        if (plan is null)
        {
            return ErroresSuscripcion.PlanNoDisponible;
        }

        var mes = PeriodoMensual.DesdeFecha(fechaInicioUtc);
        var periodo = await _repositorio.ObtenerPeriodoConBloqueoAsync(request.EmpresaId, mes, cancellationToken);
        if (periodo is null)
        {
            periodo = PeriodoConsumo.Abrir(suscripcion, mes, plan.MinutosIncluidos);
            _repositorio.Agregar(periodo);
            await _repositorio.GuardarCambiosAsync(cancellationToken);
        }

        var totales = await _repositorio.ObtenerTotalesConsumoAsync(request.EmpresaId, periodo.Id, cancellationToken);
        var saldo = new SaldoConsumo(
            periodo.MinutosContratados,
            totales.MinutosReservados,
            totales.MinutosFacturables);

        var minutosAReservar = ConsumoLlamada.CalcularMinutos(request.DuracionSeg);
        if (!saldo.AdmiteReserva(minutosAReservar, suscripcion.PermiteExcedente))
        {
            return ErroresSuscripcion.CuotaAgotada;
        }

        ConsumoLlamada consumo;
        if (existente is not null)
        {
            existente.Reactivar(periodo, request.DuracionSeg);
            consumo = existente;
        }
        else
        {
            consumo = ConsumoLlamada.Reservar(periodo, request.LlamadaId, request.DuracionSeg);
            _repositorio.Agregar(consumo);
        }

        await _repositorio.GuardarCambiosAsync(cancellationToken);
        await transaccion.ConfirmarAsync(cancellationToken);

        return consumo.AConsumoLlamadaDto();
    }

    public async Task<Resultado> RegistrarUsoTecnicoAsync(
        RegistrarUsoTecnicoRequest request,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validadorUsoTecnico.ValidarAsync(request, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        var consumo = await _repositorio.ObtenerConsumoLlamadaAsync(
            request.EmpresaId,
            request.LlamadaId,
            cancellationToken);

        if (consumo is null)
        {
            return ErroresSuscripcion.ConsumoNoEncontrado;
        }

        consumo.AcumularCostoTecnico(
            request.TokensTranscriptor,
            request.TokensEntradaEvaluador,
            request.TokensSalidaEvaluador,
            request.CostoEstimado);

        await _repositorio.GuardarCambiosAsync(cancellationToken);
        return Resultado.Exito();
    }

    public async Task<Resultado> FacturarAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty || llamadaId == Guid.Empty)
        {
            return ErrorAplicacion.Validacion(new Dictionary<string, string[]>
            {
                ["general"] = ["La empresa y la llamada son obligatorias."],
            });
        }

        var consumo = await _repositorio.ObtenerConsumoLlamadaAsync(empresaId, llamadaId, cancellationToken);
        if (consumo is null)
        {
            return ErroresSuscripcion.ConsumoNoEncontrado;
        }

        if (consumo.Estado == EstadoConsumo.Liberado)
        {
            return ErrorAplicacion.Conflicto(
                "CONSUMO_YA_LIBERADO",
                "Un consumo liberado por fallo definitivo no puede facturarse.");
        }

        if (consumo.Estado != EstadoConsumo.Facturado)
        {
            consumo.Facturar();
            await _repositorio.GuardarCambiosAsync(cancellationToken);
        }

        return Resultado.Exito();
    }

    public async Task<Resultado> LiberarAsync(
        Guid empresaId,
        Guid llamadaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty || llamadaId == Guid.Empty)
        {
            return ErrorAplicacion.Validacion(new Dictionary<string, string[]>
            {
                ["general"] = ["La empresa y la llamada son obligatorias."],
            });
        }

        var consumo = await _repositorio.ObtenerConsumoLlamadaAsync(empresaId, llamadaId, cancellationToken);
        if (consumo is null)
        {
            return ErroresSuscripcion.ConsumoNoEncontrado;
        }

        if (consumo.Estado == EstadoConsumo.Facturado)
        {
            return ErrorAplicacion.Conflicto(
                "CONSUMO_YA_FACTURADO",
                "Un consumo ya facturado no puede liberarse.");
        }

        if (consumo.Estado != EstadoConsumo.Liberado)
        {
            consumo.Liberar();
            await _repositorio.GuardarCambiosAsync(cancellationToken);
        }

        return Resultado.Exito();
    }
}
