using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Features.Autenticacion;
using Qualia.Application.Features.Campanas;
using Qualia.Application.Features.Disputas;
using Qualia.Application.Features.Empresas;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Desempeno;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Revision;
using Qualia.Application.Features.Suscripciones.Consumo.ConsultarConsumo;
using Qualia.Application.Features.Usuarios;
using Qualia.Application.Features.Suscripciones.Consumo.ConsumoEvaluacion;
using Qualia.Application.Features.Suscripciones.CrearSuscripcionInicial;
using Qualia.Application.Features.Suscripciones.ObtenerSuscripcion;
using Qualia.Application.Features.Suscripciones.PagoSimulado;
using Qualia.Application.Features.Suscripciones.Planes;
using Qualia.Application.Features.Suscripciones.Plataforma.CambiarSuscripcion;
using Qualia.Application.Features.Suscripciones.Plataforma.ListarEmpresasComerciales;

namespace Qualia.Application;

/// Composición de la capa de aplicación. Los módulos agregan sus
/// validadores de FluentValidation y servicios de aplicación aquí,
/// sin que la API conozca implementaciones concretas.
public static class DependencyInjection
{
    /// configuration: Opcional: sin configuración se usan los valores por defecto de las opciones.
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Registra todos los validadores FluentValidation de esta capa.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.TryAddSingleton(TimeProvider.System);

        // --- planes, suscripciones y consumo ---
        RegistrarSuscripciones(services);

        // --- llamadas y procesamiento ---
        if (configuration is not null)
        {
            services.Configure<OpcionesProcesamiento>(configuration.GetSection(OpcionesProcesamiento.Seccion));
        }
        else
        {
            services.AddOptions<OpcionesProcesamiento>();
        }

        services.AddScoped<AlcanceLlamadas>();
        services.AddScoped<ConsultasLlamadas>();
        services.AddScoped<IniciarEvaluacion>();
        services.AddScoped<CargarLlamada>();
        services.AddScoped<EncoladorEvaluacion>();
        services.AddScoped<ProcesadorTrabajo>();
        services.AddSingleton<EjecutorProcesamiento>();
        services.AddSingleton<DespachadorEvaluacionAutomatica>();

        // --- desempeño: scorecards y reportes ---
        services.AddScoped<ConsultasDesempeno>();

        // --- identidad: registro de empresa e inicio de sesión ---
        services.AddScoped<RegistroEmpresa>();
        services.AddScoped<InicioSesion>();
        services.AddScoped<GestionUsuarios>();
        services.AddScoped<GestionDisputas>();

        // --- campañas, rúbricas y revisión humana ---
        services.AddScoped<GestionCampanas>();
        services.AddScoped<GestionRubricas>();
        services.AddScoped<ServicioRevision>();
        services.AddScoped<IRevisionEvaluacion>(sp => sp.GetRequiredService<ServicioRevision>()); // contrato B → A
        services.AddScoped<IConsultaRevisionVigente, ConsultaRevisionVigente>();                  // contrato B → D/C
        services.AddScoped<RevisionEvaluaciones>();

        return services;
    }

    /// Registra los casos de uso y contratos compartidos del módulo de suscripciones.
    /// Requiere que la capa de infraestructura registre IRepositorioSuscripciones e IContextoEmpresa.
    public static IServiceCollection AddSuscripcionesApplication(this IServiceCollection services) =>
        services.AddApplication();

    private static void RegistrarSuscripciones(IServiceCollection services)
    {
        services.TryAddScoped<ICrearSuscripcionInicial, CrearSuscripcionInicialService>(); // contrato C → A
        services.TryAddScoped<IConsultaPlanService, ConsultaPlanService>();                 // contrato C → B
        services.TryAddScoped<IConsumoEvaluacion, ConsumoEvaluacionService>();             // contrato C → D

        services.TryAddScoped<ListarPlanesHandler>();
        services.TryAddScoped<ObtenerSuscripcionHandler>();
        services.TryAddScoped<ConsultarConsumoHandler>();
        services.TryAddScoped<RegistrarPagoSimuladoHandler>();
        services.TryAddScoped<ListarEmpresasComercialesHandler>();
        services.TryAddScoped<CambiarSuscripcionHandler>();
    }
}
