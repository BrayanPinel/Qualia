using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;
using Qualia.Application.Common.Seguridad;

namespace Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;

/// Caso de uso GET /api/empresas/{empresaId}/suscripcion.
/// Devuelve la suscripción vigente o, si no hay, la última vencida.
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
        if (_contextoEmpresa.EmpresaIdActual is not { } empresaId || empresaId == Guid.Empty)
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
