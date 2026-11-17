using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Interfaces;
using Qualia.Application.Common.Seguridad;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Audio;
using Qualia.Infrastructure.Persistence;
using Qualia.Infrastructure.Persistence.Semillas;
using Qualia.Infrastructure.Procesamiento;
using Qualia.Infrastructure.Provisional;
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

        // --- Rol D: motor de evaluación (modo Simulado) ---
        services.AddSingleton<IEnmascaradorDatos, EnmascaradorDatos>();
        services.AddSingleton<ITranscriptor, TranscriptorSimulado>();
        services.AddSingleton<IEvaluadorIA, EvaluadorIASimulado>();
        services.AddSingleton<ISenalProcesamiento, SenalProcesamiento>();

        var opcionesProcesamiento = configuration.GetSection(OpcionesProcesamiento.Seccion).Get<OpcionesProcesamiento>()
                                    ?? new OpcionesProcesamiento();
        if (opcionesProcesamiento.WorkerHabilitado)
        {
            services.AddHostedService<WorkerProcesamiento>();
        }

        // --- Rol D: audio ---
        services.Configure<OpcionesAudio>(configuration.GetSection(OpcionesAudio.Seccion));
        services.AddSingleton<IGeneradorUrlAudio, GeneradorUrlAudioFirmada>();
        services.AddSingleton<IAlmacenamientoAudio, AlmacenamientoAudioLocal>();

        // --- Contratos con otros roles: PROVISIONALES hasta integrar sus módulos ---
        services.AddScoped<IConsumoEvaluacion, ConsumoEvaluacionProvisional>();          // Rol C
        services.AddScoped<IRegistroAuditoria, RegistroAuditoriaProvisional>();          // Rol A
        services.AddScoped<IConsultaRevisionVigente, ConsultaRevisionVigenteProvisional>(); // Rol B

        return services;
    }
}
