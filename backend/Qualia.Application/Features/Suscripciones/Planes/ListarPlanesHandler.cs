using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Features.Suscripciones.Planes;

/// Caso de uso público GET /api/planes 
public sealed class ListarPlanesHandler
{
    private readonly IRepositorioSuscripciones _repositorio;

    public ListarPlanesHandler(IRepositorioSuscripciones repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<PlanDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var planes = await _repositorio.ListarPlanesActivosAsync(cancellationToken);
        return planes.Select(p => p.APlanDto()).ToArray();
    }
}
