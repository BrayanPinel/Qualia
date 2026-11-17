using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Planes;

/// <summary>
/// Implementación del contrato compartido <see cref="IConsultaPlanService"/>.
/// Permite a otros módulos consultar límites y funciones del plan contratado
/// sin acoplarse a entidades internas de suscripción.
/// </summary>
public sealed class ConsultaPlanService : IConsultaPlanService
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly TimeProvider _timeProvider;

    public ConsultaPlanService(IRepositorioSuscripciones repositorio, TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _timeProvider = timeProvider;
    }

    public async Task<bool> TieneFuncionAsync(
        Guid empresaId,
        string codigoFuncion,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty || string.IsNullOrWhiteSpace(codigoFuncion))
        {
            return false;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
        if (suscripcion?.Plan is null || !suscripcion.EsVigente)
        {
            return false;
        }

        return suscripcion.Plan.TieneFuncion(codigoFuncion);
    }

    public async Task<bool> AdmiteSupervisoresAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty || totalResultante < 0)
        {
            return false;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
        if (suscripcion?.Plan is null || !suscripcion.EsVigente)
        {
            return false;
        }

        return suscripcion.Plan.AdmiteSupervisores(totalResultante);
    }

    public async Task<bool> AdmiteRubricasActivasAsync(
        Guid empresaId,
        int totalResultante,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty || totalResultante < 0)
        {
            return false;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
        if (suscripcion?.Plan is null || !suscripcion.EsVigente)
        {
            return false;
        }

        return suscripcion.Plan.AdmiteRubricasActivas(totalResultante);
    }

    public async Task<Resultado<SuscripcionDto>> VerificarSuscripcionOperativaAsync(
        Guid empresaId,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty)
        {
            return ErroresSuscripcion.EmpresaNoIdentificada;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualSinContextoAsync(empresaId, cancellationToken);
        if (suscripcion is null)
        {
            return ErroresSuscripcion.SuscripcionNoEncontrada;
        }

        var ahoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
        if (!suscripcion.PermiteNuevasEvaluaciones(ahoraUtc))
        {
            return ErroresSuscripcion.SuscripcionNoActiva;
        }

        var mesActual = PeriodoMensual.DesdeFecha(ahoraUtc);
        var periodoActual = await _repositorio.ObtenerPeriodoAsync(empresaId, mesActual, cancellationToken);
        return suscripcion.ASuscripcionDto(periodoActual);
    }
}
