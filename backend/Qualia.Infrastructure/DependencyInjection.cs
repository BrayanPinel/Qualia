using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Application.Features.Suscripciones.Comun;
using Qualia.Infrastructure.Audio;
using Qualia.Infrastructure.IA;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Repositories;
using Qualia.Infrastructure.Persistence.Seeders;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Infrastructure.Procesamiento;
using Qualia.Infrastructure.Seguridad;

namespace Qualia.Infrastructure;

/// <summary>
/// Composición de infraestructura. La cadena de conexión se valida al
/// resolver el DbContext, no al arrancar la API, para que el arranque
/// funcione sin una base de datos levantada.
/// </summary>
public static class DependencyInjection
{
    public const string NombreCadenaConexion = "PostgresConnection";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Persistencia y aislamiento multiempresa ---
        services.AddScoped<IContextoEmpresa, ContextoEmpresa>();
        services.AddDbContext<QualiaDbContext>(opciones =>
        {
            var cadenaConexion = configuration.GetConnectionString(NombreCadenaConexion);
            if (string.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new InvalidOperationException(
                    $"Falta la cadena de conexión 'ConnectionStrings:{NombreCadenaConexion}' (backend/Qualia.API/.env).");
            }

            opciones
                .UseNpgsql(cadenaConexion, npgsql => npgsql
                    .MigrationsAssembly(typeof(QualiaDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__ef_migrations_history", QualiaDbContext.Esquema))
                .UseSnakeCaseNamingConvention();
        });
        services.AddScoped<IQualiaDbContext>(sp => sp.GetRequiredService<QualiaDbContext>());
        services.AddScoped<SembradorDatosDemo>();

        // --- identidad: hash de contraseñas ---
        services.Configure<OpcionesHashContrasena>(configuration.GetSection(OpcionesHashContrasena.Seccion));
        services.AddSingleton<IServicioHashContrasena, ServicioHashContrasena>();

        // --- auditoría empresarial (contrato compartido con todos los módulos) ---
        services.AddScoped<IRegistroAuditoria, RegistroAuditoria>();

        // --- motor de evaluación (IA:Modo = Simulado | Real) ---
        services.AddSingleton<IEnmascaradorDatos, EnmascaradorDatos>();
        services.AddSingleton<ISenalProcesamiento, SenalProcesamiento>();
        AgregarProveedoresIA(services, configuration);

        var opcionesProcesamiento = configuration.GetSection(OpcionesProcesamiento.Seccion).Get<OpcionesProcesamiento>()
                                    ?? new OpcionesProcesamiento();
        if (opcionesProcesamiento.WorkerHabilitado)
        {
            services.AddHostedService<WorkerProcesamiento>();
        }

        // --- audio ---
        services.Configure<OpcionesAudio>(configuration.GetSection(OpcionesAudio.Seccion));
        services.AddSingleton<IGeneradorUrlAudio, GeneradorUrlAudioFirmada>();
        services.AddSingleton<IAlmacenamientoAudio, AlmacenamientoAudioLocal>();

        // --- planes, suscripciones y consumo ---
        // Los contratos IConsumoEvaluacion e IConsultaPlanService los registra Application con sus servicios reales.
        services.AddScoped<IRepositorioSuscripciones, RepositorioSuscripciones>();
        services.AddScoped<PlanesSeeder>();


        return services;
    }

    /// <summary>
    /// Simulado: transcripción desde el ground data y evaluación por palabras clave.
    /// Real: Deepgram (transcripción) y Groq (actores y evaluación).
    /// Las llaves se leen del .env (Deepgram__ApiKey y Groq__ApiKey).
    /// </summary>
    private static void AgregarProveedoresIA(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpcionesIA>(configuration.GetSection(OpcionesIA.Seccion));
        services.Configure<OpcionesDeepgram>(configuration.GetSection(OpcionesDeepgram.Seccion));
        services.Configure<OpcionesGroq>(configuration.GetSection(OpcionesGroq.Seccion));
        services.AddSingleton<LectorGroundData>();
        services.AddSingleton<AlmacenRespuestasIA>();

        var ia = configuration.GetSection(OpcionesIA.Seccion).Get<OpcionesIA>() ?? new OpcionesIA();
        var deepgram = configuration.GetSection(OpcionesDeepgram.Seccion).Get<OpcionesDeepgram>() ?? new OpcionesDeepgram();
        var groq = configuration.GetSection(OpcionesGroq.Seccion).Get<OpcionesGroq>() ?? new OpcionesGroq();
        var faltanLlaves = string.IsNullOrWhiteSpace(deepgram.ApiKey) || string.IsNullOrWhiteSpace(groq.ApiKey);

        if (ia.Modo != ModoProveedorIA.Real || faltanLlaves)
        {
            services.AddSingleton<ITranscriptor>(sp => new TranscriptorSimulado(sp.GetRequiredService<LectorGroundData>()));
            services.AddSingleton<IEvaluadorIA, EvaluadorIASimulado>();
            services.AddHostedService(sp => new AvisoModoIA(
                sp.GetRequiredService<ILogger<AvisoModoIA>>(), ia.Modo == ModoProveedorIA.Real && faltanLlaves));
            return;
        }

        services.AddHttpClient<ClienteGroq>(http =>
        {
            http.BaseAddress = new Uri(groq.UrlBase);
            http.Timeout = TimeSpan.FromSeconds(groq.TiempoEsperaSeg);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", groq.ApiKey);
        });
        services.AddHttpClient<TranscriptorDeepgram>(http =>
        {
            http.BaseAddress = new Uri(deepgram.UrlBase);
            http.Timeout = TimeSpan.FromSeconds(deepgram.TiempoEsperaSeg);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", deepgram.ApiKey);
        });
        services.AddTransient<IdentificadorActoresGroq>();
        services.AddTransient<ITranscriptor>(sp => sp.GetRequiredService<TranscriptorDeepgram>());
        services.AddTransient<IEvaluadorIA, EvaluadorGroq>();
        services.AddHostedService(sp => new AvisoModoIA(sp.GetRequiredService<ILogger<AvisoModoIA>>(), faltanLlaves: false, real: true));
    }
}

/// <summary>Deja constancia en el log del modo de IA activo al iniciar la API.</summary>
internal sealed class AvisoModoIA(ILogger<AvisoModoIA> logger, bool faltanLlaves, bool real = false) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (faltanLlaves)
        {
            logger.LogWarning("IA:Modo=Real pero faltan Deepgram__ApiKey o Groq__ApiKey en el .env: se usa el modo Simulado.");
        }
        else
        {
            logger.LogInformation("Motor de IA en modo {Modo}", real ? "Real (Deepgram + Groq)" : "Simulado");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
