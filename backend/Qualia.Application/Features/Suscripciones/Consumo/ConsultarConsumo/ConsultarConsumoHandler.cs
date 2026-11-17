using FluentValidation;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;

/// <summary>
/// Caso de uso <c>GET /api/consumo?periodo=YYYY-MM</c> (fase1 §7.2 y §7.8).
/// Un mes sin periodo devuelve cantidades en cero.
/// </summary>
public sealed class ConsultarConsumoHandler
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IContextoEmpresa _contextoEmpresa;
    private readonly IValidator<ConsultarConsumoQuery> _validador;
    private readonly TimeProvider _timeProvider;

    public ConsultarConsumoHandler(
        IRepositorioSuscripciones repositorio,
        IContextoEmpresa contextoEmpresa,
        IValidator<ConsultarConsumoQuery> validador,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _contextoEmpresa = contextoEmpresa;
        _validador = validador;
        _timeProvider = timeProvider;
    }

    public async Task<Resultado<ConsumoDto>> EjecutarAsync(
        ConsultarConsumoQuery query,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validador.ValidarAsync(query, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        if (_contextoEmpresa.EmpresaId is not { } empresaId || empresaId == Guid.Empty)
        {
            return ErroresSuscripcion.EmpresaNoIdentificada;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualAsync(empresaId, cancellationToken);
        if (suscripcion is null)
        {
            return ErroresSuscripcion.SuscripcionNoEncontrada;
        }

        var mes = string.IsNullOrWhiteSpace(query.Periodo)
            ? PeriodoMensual.DesdeFecha(_timeProvider.GetUtcNow().UtcDateTime)
            : PeriodoMensual.TryParse(query.Periodo.Trim(), out var parsed)
                ? parsed
                : throw new InvalidOperationException("El validador debería haber rechazado un periodo inválido.");

        var periodo = await _repositorio.ObtenerPeriodoAsync(empresaId, mes, cancellationToken);
        if (periodo is null)
        {
            return SaldoConsumo.Vacio.AConsumoDto(empresaId, mes);
        }

        var totales = await _repositorio.ObtenerTotalesConsumoAsync(empresaId, periodo.Id, cancellationToken);
        var saldo = new SaldoConsumo(
            periodo.MinutosContratados,
            totales.MinutosReservados,
            totales.MinutosFacturables);

        return saldo.AConsumoDto(empresaId, mes);
    }
}
