using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Campanas;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Revision;

namespace Qualia.Application;

/// <summary>
/// Composición de la capa de aplicación. Los módulos agregan sus
/// validadores de FluentValidation y servicios de aplicación aquí,
/// sin que la API conozca implementaciones concretas.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Registra todos los validadores FluentValidation de esta capa.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);

        // --- llamadas y procesamiento ---
        services.Configure<OpcionesProcesamiento>(configuration.GetSection(OpcionesProcesamiento.Seccion));
        services.AddScoped<AlcanceLlamadas>();
        services.AddScoped<ConsultasLlamadas>();
        services.AddScoped<IniciarEvaluacion>();
        services.AddScoped<ProcesadorTrabajo>();
        services.AddSingleton<EjecutorProcesamiento>();

        // --- campañas, rúbricas y revisión humana ---
        services.AddScoped<GestionCampanas>();
        services.AddScoped<GestionRubricas>();
        services.AddScoped<ServicioRevision>();
        services.AddScoped<IRevisionEvaluacion>(sp => sp.GetRequiredService<ServicioRevision>()); // contrato B → A
        services.AddScoped<IConsultaRevisionVigente, ConsultaRevisionVigente>();                  // contrato B → D/C
        services.AddScoped<RevisionEvaluaciones>();

        return services;
    }
}
