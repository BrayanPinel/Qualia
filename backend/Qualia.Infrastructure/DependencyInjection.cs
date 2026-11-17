using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Comun.Contratos;
using Qualia.Application.Comun.Persistencia;
using Qualia.Application.Comun.Seguridad;
using Qualia.Application.Features.Llamadas;
using Qualia.Application.Features.Procesamiento;
using Qualia.Infrastructure.Audio;
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
