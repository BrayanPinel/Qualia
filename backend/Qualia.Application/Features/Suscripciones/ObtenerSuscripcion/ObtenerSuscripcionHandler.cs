using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;

/// <summary>
/// Caso de uso <c>GET /api/suscripcion</c> (fase1 ).
/// Devuelve la suscripción vigente o, si no hay, la última vencida.
/// </summary>
public sealed class ObtenerSuscripcionHandler
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IContextoEmpresa _contextoEmpresa;
    private readonly TimeProvider _timeProvider;

    public ObtenerSuscripcionHandler(
        IRepositorioSuscripciones repositorio,
        IContextoEmpresa contextoEmpresa,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _contextoEmpresa = contextoEmpresa;
        _timeProvider = timeProvider;
    }

    public async Task<Resultado<SuscripcionDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        if (_contextoEmpresa.EmpresaId is not { } empresaId || empresaId == Guid.Empty)
        {
            return ErroresSuscripcion.EmpresaNoIdentificada;
        }

        var suscripcion = await _repositorio.ObtenerSuscripcionActualAsync(empresaId, cancellationToken);
        if (suscripcion is null)
        {
            return ErroresSuscripcion.SuscripcionNoEncontrada;
        }

        var mesActual = PeriodoMensual.DesdeFecha(_timeProvider.GetUtcNow().UtcDateTime);
        var periodoActual = await _repositorio.ObtenerPeriodoAsync(empresaId, mesActual, cancellationToken);

        return suscripcion.ASuscripcionDto(periodoActual);
    }
}
