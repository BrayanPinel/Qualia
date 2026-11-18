using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;
using Qualia.Domain.Servicios;
using Qualia.Infrastructure.Persistence;
using Qualia.Tests.Integracion.Infraestructura;

namespace Qualia.Tests.Integracion;

/// <summary>
/// Restricciones únicas de identidad contra PostgreSQL real, con los valores ya normalizados:
/// correo único por empresa (no global), código de empresa y correo de administrador únicos globales.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public sealed class UnicidadIdentidadIntegracionTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string ViolacionUnica = "23505";
    private readonly string _baseDatos = $"qualia_identidad_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        if (!DockerDisponibleParaPruebas())
        {
            return;
        }

        await using var db = Crear();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (!DockerDisponibleParaPruebas())
        {
            return;
        }

        await using var db = Crear();
        await db.Database.EnsureDeletedAsync();
    }

    [FactConDocker]
    public async Task El_mismo_correo_es_valido_en_dos_empresas()
    {
        var norte = await CrearEmpresaAsync("norte");
        var sur = await CrearEmpresaAsync("sur");

        await using var db = Crear();
        db.Usuarios.AddRange(NuevoUsuario(norte, "  Compartido@Correo.com"), NuevoUsuario(sur, "compartido@correo.COM "));
        await db.SaveChangesAsync();

        var correos = await db.Usuarios.IgnoreQueryFilters()   // la prueba compara ambas empresas a propósito
            .Where(u => u.EmpresaId == norte || u.EmpresaId == sur)
            .Select(u => u.Correo)
            .ToListAsync();
        Assert.Equal(["compartido@correo.com", "compartido@correo.com"], correos);
    }

    [FactConDocker]
    public async Task El_mismo_correo_con_otra_capitalizacion_se_rechaza_dentro_de_una_empresa()
    {
        var norte = await CrearEmpresaAsync("norte");

        await using (var db = Crear())
        {
            db.Usuarios.Add(NuevoUsuario(norte, "ana@norte.com"));
            await db.SaveChangesAsync();
        }

        await using (var db = Crear())
        {
            db.Usuarios.Add(NuevoUsuario(norte, " ANA@Norte.com "));
            await AssertViolacionUnicaAsync(db);
        }
    }

    [FactConDocker]
    public async Task El_codigo_de_empresa_es_unico_global_tras_normalizar()
    {
        await CrearEmpresaAsync("Mi-Empresa");

        await using var db = Crear();
        db.Empresas.Add(NuevaEmpresa(" mi-EMPRESA "));
        await AssertViolacionUnicaAsync(db);
    }

    [FactConDocker]
    public async Task El_correo_de_administrador_es_unico_global_tras_normalizar()
    {
        await using (var db = Crear())
        {
            db.Administradores.Add(NuevoAdministrador("root@qualia.com"));
            await db.SaveChangesAsync();
        }

        await using (var db = Crear())
        {
            db.Administradores.Add(NuevoAdministrador("ROOT@Qualia.com"));
            await AssertViolacionUnicaAsync(db);
        }
    }

    private QualiaDbContext Crear()
    {
        var opciones = new DbContextOptionsBuilder<QualiaDbContext>()
            .UseNpgsql(postgres.CadenaConexion(_baseDatos))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new QualiaDbContext(opciones, new SinEmpresa());
    }

    private async Task<Guid> CrearEmpresaAsync(string codigo)
    {
        await using var db = Crear();
        var empresa = NuevaEmpresa(codigo);
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
        return empresa.Id;
    }

    private static Empresa NuevaEmpresa(string codigo) => new()
    {
        Codigo = NormalizadorIdentidad.Codigo(codigo),
        Nombre = $"Empresa {codigo.Trim()}",
        FechaRegistro = DateTime.UtcNow,
    };

    private static Usuario NuevoUsuario(Guid empresaId, string correo) => new()
    {
        EmpresaId = empresaId,
        Nombre = "Usuario de prueba",
        Correo = NormalizadorIdentidad.Correo(correo),
        Rol = RolEmpresa.Agente,
        PasswordHash = "hash-de-prueba",
    };

    private static Administrador NuevoAdministrador(string correo) => new()
    {
        Nombre = "Administrador de prueba",
        Correo = NormalizadorIdentidad.Correo(correo),
        PasswordHash = "hash-de-prueba",
    };

    private static async Task AssertViolacionUnicaAsync(QualiaDbContext db)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(ViolacionUnica, postgresError.SqlState);
    }

    private static bool DockerDisponibleParaPruebas() => new FactConDockerAttribute().Skip is null;

    private sealed class SinEmpresa : IContextoEmpresa
    {
        public Guid? EmpresaIdActual => null;

        public void Establecer(Guid empresaId) => throw new NotSupportedException();
    }
}
