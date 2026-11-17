using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qualia.Application.Comun.Contratos;
using Qualia.Application.Comun.Persistencia;
using Qualia.Application.Comun.Seguridad;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Audio;
using Qualia.Infrastructure.IA;
using Qualia.Infrastructure.Persistencia;
using Qualia.Infrastructure.Persistencia.Semillas;
using Qualia.Infrastructure.Procesamiento;
using Qualia.Infrastructure.Provisional;
using Qualia.Infrastructure.Seguridad;

namespace Qualia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var cadenaConexion = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__DefaultConnection (backend/Qualia.API/.env).");
        }

        // --- Persistencia y aislamiento multiempresa ---
        services.AddScoped<IContextoEmpresa, ContextoEmpresa>();
        services.AddDbContext<QualiaDbContext>(opciones => opciones
            .UseNpgsql(cadenaConexion, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", QualiaDbContext.Esquema))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IQualiaDbContext>(sp => sp.GetRequiredService<QualiaDbContext>());
        services.AddScoped<SembradorDatosDemo>();

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

        // --- Contratos con otros roles: PROVISIONALES hasta integrar sus módulos ---
        services.AddScoped<IConsumoEvaluacion, ConsumoEvaluacionProvisional>();          
        services.AddScoped<IRegistroAuditoria, RegistroAuditoriaProvisional>();          
        services.AddScoped<IConsultaRevisionVigente, ConsultaRevisionVigenteProvisional>(); 

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
