using Qualia.Application.Features.Suscripciones.Comun;

namespace Qualia.Application.Features.Suscripciones.Planes;

/// <summary>
/// Caso de uso público GET /api/planes 
/// </summary>
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
