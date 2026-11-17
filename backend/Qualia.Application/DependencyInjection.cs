using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Features.Suscripciones.Consumo.ConsumoEvaluacion;
using Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Features.Suscripciones.Planes;
using Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;
using Qualia.Application.Features.Suscripciones.Plataforma.ListarEmpresasComerciales;

namespace Qualia.Application;

/// <summary>
/// Composición de la capa de aplicación. Los módulos agregan sus
/// validadores de FluentValidation y servicios de aplicación aquí,
/// sin que la API conozca implementaciones concretas.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Registra los casos de uso y contratos compartidos del módulo de suscripciones.
    /// Requiere que la capa de infraestructura registre <c>IRepositorioSuscripciones</c>
    /// y <c>IContextoEmpresa</c>.
    /// </summary>
    public static IServiceCollection AddSuscripcionesApplication(this IServiceCollection services)
    {
        services.AddApplication();

        services.AddScoped<ICrearSuscripcionInicial, CrearSuscripcionInicialService>();
        services.AddScoped<IConsultaPlanService, ConsultaPlanService>();
        services.AddScoped<IConsumoEvaluacion, ConsumoEvaluacionService>();

        services.AddScoped<ListarPlanesHandler>();
        services.AddScoped<ObtenerSuscripcionHandler>();
        services.AddScoped<ConsultarConsumoHandler>();
        services.AddScoped<RegistrarPagoSimuladoHandler>();
        services.AddScoped<ListarEmpresasComercialesHandler>();
        services.AddScoped<CambiarSuscripcionHandler>();

        return services;
    }
}
