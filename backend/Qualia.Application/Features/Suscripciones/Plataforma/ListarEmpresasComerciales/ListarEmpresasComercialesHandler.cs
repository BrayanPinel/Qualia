using FluentValidation;
using Qualia.Application.Common.Models;
using Qualia.Application.Common.Resultados;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Domain.Suscripciones;

namespace Qualia.Application.Features.Suscripciones.Plataforma.ListarEmpresasComerciales;

/// <summary>
/// Caso de uso GET /api/plataforma/empresas (HU-07).
/// Devuelve únicamente proyección comercial sin acceso a contenido de llamadas.
/// </summary>
public sealed class ListarEmpresasComercialesHandler
{
    private readonly IRepositorioSuscripciones _repositorio;
    private readonly IValidator<PaginacionQuery> _validador;
    private readonly TimeProvider _timeProvider;

    public ListarEmpresasComercialesHandler(
        IRepositorioSuscripciones repositorio,
        IValidator<PaginacionQuery> validador,
        TimeProvider timeProvider)
    {
        _repositorio = repositorio;
        _validador = validador;
        _timeProvider = timeProvider;
    }

    public async Task<Resultado<PaginaDto<EmpresaComercialDto>>> EjecutarAsync(
        PaginacionQuery query,
        CancellationToken cancellationToken = default)
    {
        var errorValidacion = await _validador.ValidarAsync(query, cancellationToken);
        if (errorValidacion is not null)
        {
            return errorValidacion;
        }

        var mesActual = PeriodoMensual.DesdeFecha(_timeProvider.GetUtcNow().UtcDateTime);
        var (items, total) = await _repositorio.ListarEmpresasComercialesAsync(
            query.Pagina,
            query.TamanoPagina,
            mesActual,
            cancellationToken);

        return new PaginaDto<EmpresaComercialDto>(items, query.Pagina, query.TamanoPagina, total);
    }
}
