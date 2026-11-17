using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qualia.Infrastructure.Persistencia.Semillas;

namespace Qualia.Infrastructure.Persistencia;

/// <summary>
/// Preparación de la base de datos al arrancar (solo desarrollo, controlado por configuración):
/// BaseDatos:AplicarEsquemaAlIniciar: aplica las migraciones Code First. Si el proyecto
/// aún no tiene migraciones (la oficial la integra un solo responsable), usa EnsureCreated.
/// BaseDatos:SembrarDatosDemo: precarga las llamadas ficticias.
/// </summary>
public static class InicializadorBaseDatos
{
    public static async Task InicializarAsync(IServiceProvider servicios, IConfiguration configuracion, CancellationToken ct = default)
    {
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QualiaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(InicializadorBaseDatos));

        if (configuracion.GetValue<bool>("BaseDatos:AplicarEsquemaAlIniciar"))
        {
            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync(ct);
                logger.LogInformation("Migraciones aplicadas");
            }
            else
            {
                await db.Database.EnsureCreatedAsync(ct);
                logger.LogWarning("No hay migraciones en el proyecto: esquema creado con EnsureCreated (solo desarrollo)");
            }
        }

        if (configuracion.GetValue<bool>("BaseDatos:SembrarDatosDemo"))
        {
            await scope.ServiceProvider.GetRequiredService<SembradorDatosDemo>().SembrarAsync(ct);
        }
    }
}
