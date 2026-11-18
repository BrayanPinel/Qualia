using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qualia.Infrastructure.Persistence.Seeders;
using Qualia.Infrastructure.Persistence.Semillas;

namespace Qualia.Infrastructure.Persistence;

/// <summary>
/// Preparación de la base de datos al arrancar (solo desarrollo, controlado por configuración):
/// BaseDatos:AplicarEsquemaAlIniciar: aplica las migraciones Code First (Persistence/Migrations).
/// Si el proyecto no tuviera migraciones, usa EnsureCreated.
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
                await VerificarBaseMigrableAsync(db, ct);
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
    /// Una base creada antes con EnsureCreated tiene las tablas pero no el historial de migraciones:
    /// MigrateAsync intentaría crearlas de nuevo y fallaría con "relation already exists".
    /// Se detiene el arranque con la instrucción para recrearla (solo datos de desarrollo).
    /// </summary>
    private static async Task VerificarBaseMigrableAsync(QualiaDbContext db, CancellationToken ct)
    {
        // Base inexistente: MigrateAsync la crea desde cero.
        if (!await db.Database.CanConnectAsync(ct) || (await db.Database.GetAppliedMigrationsAsync(ct)).Any())
        {
            return;
        }

        var tablas = await TablasExistentesAsync(db, ct);
        if (tablas.Any(t => !t.StartsWith("__", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "La base de datos de desarrollo se creó sin migraciones (EnsureCreated) y no puede migrarse. " +
                "Recree la base local una sola vez: cd infra; docker compose down -v; docker compose up -d --wait");
        }
    }

    private static Task<List<string>> TablasExistentesAsync(QualiaDbContext db, CancellationToken ct) =>
        db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = {0}", QualiaDbContext.Esquema)
            .ToListAsync(ct);

    /// <summary>
    /// EnsureCreated no agrega tablas a una base que ya existe. Si un módulo nuevo agregó entidades,
    /// la base de desarrollo queda incompleta: se detiene el arranque con instrucciones claras.
    /// </summary>
    private static async Task VerificarTablasAsync(QualiaDbContext db, CancellationToken ct)
    {
        var existentes = await TablasExistentesAsync(db, ct);

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
