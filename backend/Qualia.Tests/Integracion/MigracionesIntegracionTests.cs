using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qualia.Application.Common.Seguridad;
using Qualia.Infrastructure;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;

namespace Qualia.Tests.Integracion;

/// Migración oficial: coincide con el modelo, crea las 24 tablas del diagrama desde una base vacía,
/// es idempotente y una base antigua creada con EnsureCreated se detecta con un mensaje claro.
[Collection(ColeccionPostgres.Nombre)]
public sealed class MigracionesIntegracionTests(PostgresFixture postgres)
{
    private static readonly string[] TablasDiagrama =
    [
        "administrador", "auditoria", "auditoria_plataforma", "campana", "consumo_llamada", "criterio_revisado",
        "criterio_rubrica", "detalle_evaluacion", "disputa", "empresa", "evaluacion_ia", "evidencia_criterio",
        "evidencia_revision", "funcion_plan", "llamada", "periodo_consumo", "plan", "revision_humana", "rubrica",
        "segmento_transcripcion", "suscripcion", "trabajo_procesamiento", "usuario", "usuario_campana",
    ];

    [Fact]
    public void El_modelo_no_tiene_cambios_sin_migrar()
    {
        // Si falla: alguien cambió entidades o configuraciones sin agregar una migración nueva.
        using var db = Crear("Host=localhost;Database=sin_conexion;Username=x;Password=x");

        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Contains(db.Database.GetMigrations(), m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [FactConDocker]
    public async Task La_migracion_crea_las_24_tablas_desde_una_base_vacia_y_es_idempotente()
    {
        await using var db = Crear(postgres.CadenaConexion($"qualia_migracion_{Guid.NewGuid():N}"));
        try
        {
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var tablas = await TablasAsync(db);
            Assert.Equal(TablasDiagrama, tablas.Where(t => !t.StartsWith("__", StringComparison.Ordinal)).Order());
            Assert.Contains("__ef_migrations_history", tablas);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [FactConDocker]
    public async Task Una_base_creada_con_EnsureCreated_se_detecta_con_instrucciones()
    {
        var cadena = postgres.CadenaConexion($"qualia_antigua_{Guid.NewGuid():N}");
        await using (var antigua = Crear(cadena))
        {
            await antigua.Database.EnsureCreatedAsync();
        }

        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgresConnection"] = cadena,
            ["BaseDatos:AplicarEsquemaAlIniciar"] = "true",
        }).Build();
        var servicios = new ServiceCollection()
            .AddLogging()
            .AddScoped<IUsuarioActual, SinUsuario>()
            .AddInfrastructure(configuracion)
            .BuildServiceProvider();

        try
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => InicializadorBaseDatos.InicializarAsync(servicios, configuracion));
            Assert.Contains("docker compose down -v", error.Message);
        }
        finally
        {
            await using var db = Crear(cadena);
            await db.Database.EnsureDeletedAsync();
            await servicios.DisposeAsync();
        }
    }

    private static QualiaDbContext Crear(string cadena)
    {
        var opciones = new DbContextOptionsBuilder<QualiaDbContext>()
            .UseNpgsql(cadena, n => n
                .MigrationsAssembly(typeof(QualiaDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__ef_migrations_history", QualiaDbContext.Esquema))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new QualiaDbContext(opciones, new SinEmpresa());
    }

    private static Task<List<string>> TablasAsync(QualiaDbContext db) =>
        db.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'qualia' ORDER BY 1")
            .ToListAsync();

    private sealed class SinEmpresa : IContextoEmpresa
    {
        public Guid? EmpresaIdActual => null;

        public void Establecer(Guid empresaId) => throw new NotSupportedException();
    }

    private sealed class SinUsuario : IUsuarioActual
    {
        public bool EstaAutenticado => false;
        public Guid? UsuarioId => null;
        public Guid? EmpresaId => null;
        public string? Rol => null;
    }
}
