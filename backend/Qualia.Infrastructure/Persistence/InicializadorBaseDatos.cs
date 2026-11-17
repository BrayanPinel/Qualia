using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qualia.Infrastructure.Persistence.Seeders;
using Qualia.Infrastructure.Persistence.Semillas;

namespace Qualia.Infrastructure.Persistence;

/// <summary>
/// Preparación de la base de datos al arrancar (solo desarrollo, controlado por configuración):
/// BaseDatos:AplicarEsquemaAlIniciar: aplica las migraciones Code First. Si el proyecto
/// aún no tiene migraciones (la oficial la integra un solo responsable), usa EnsureCreated.
/// BaseDatos:SembrarDatosDemo: siembra el catálogo de planes y precarga los datos demo.
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
                await VerificarTablasAsync(db, ct);
            }
        }

        if (configuracion.GetValue<bool>("BaseDatos:SembrarDatosDemo"))
        {
            // Primero el catálogo de planes: las suscripciones demo lo referencian.
            await scope.ServiceProvider.GetRequiredService<PlanesSeeder>().SembrarAsync(ct);
            await scope.ServiceProvider.GetRequiredService<SembradorDatosDemo>().SembrarAsync(ct);
        }
    }

    /// <summary>
    /// EnsureCreated no agrega tablas a una base que ya existe. Si un módulo nuevo agregó entidades,
    /// la base de desarrollo queda incompleta: se detiene el arranque con instrucciones claras.
    /// </summary>
    private static async Task VerificarTablasAsync(QualiaDbContext db, CancellationToken ct)
    {
        var existentes = await db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = {0}", QualiaDbContext.Esquema)
            .ToListAsync(ct);

        var faltantes = db.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .OfType<string>()
            .Distinct()
            .Except(existentes, StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                $"La base de datos de desarrollo no tiene las tablas: {string.Join(", ", faltantes)}. " +
                "Sin migraciones, EnsureCreated no actualiza un esquema existente. Recree la base local: " +
                "cd infra; docker compose down -v; docker compose up -d --wait");
        }
    }
}
