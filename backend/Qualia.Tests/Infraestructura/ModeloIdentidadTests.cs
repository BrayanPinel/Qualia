using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Entidades;
using Qualia.Infrastructure.Persistence;

namespace Qualia.Tests.Infraestructura;

/// Comprueba el mapeo de las entidades de identidad en el modelo integrado:
/// tablas, claves, FK empresariales, índices únicos y filtro multiempresa. No abre conexión.
public sealed class ModeloIdentidadTests
{
    private static QualiaDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<QualiaDbContext>()
            .UseNpgsql("Host=localhost;Database=QualiaTest;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new QualiaDbContext(options, new ContextoFijo());
    }

    [Theory]
    [InlineData(typeof(Empresa), "empresa")]
    [InlineData(typeof(Usuario), "usuario")]
    [InlineData(typeof(Administrador), "administrador")]
    [InlineData(typeof(Auditoria), "auditoria")]
    public void CadaEntidadSeMapeaASuTabla(Type tipo, string tabla)
    {
        using var db = CrearContexto();

        Assert.Equal(tabla, db.Model.FindEntityType(tipo)!.GetTableName());
    }

    [Fact]
    public void AdministradorEsGlobalConCorreoUnico()
    {
        using var db = CrearContexto();
        var entidad = db.Model.FindEntityType(typeof(Administrador))!;

        Assert.Equal(["Id"], Clave(entidad));
        Assert.Empty(entidad.GetDeclaredQueryFilters());
        var indice = Assert.Single(entidad.GetIndexes(), i => i.IsUnique);
        Assert.Equal([nameof(Administrador.Correo)], indice.Properties.Select(p => p.Name));
        Assert.Equal("password_hash", entidad.FindProperty(nameof(Administrador.PasswordHash))!.GetColumnName());
    }

    [Fact]
    public void AuditoriaEsEmpresarialConFkCompuestaOpcionalAUsuario()
    {
        using var db = CrearContexto();
        var entidad = db.Model.FindEntityType(typeof(Auditoria))!;

        Assert.Equal([nameof(Auditoria.EmpresaId), nameof(Auditoria.Id)], Clave(entidad));
        Assert.NotEmpty(entidad.GetDeclaredQueryFilters());

        var fkEmpresa = Assert.Single(entidad.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(Empresa));
        Assert.Equal([nameof(Auditoria.EmpresaId)], fkEmpresa.Properties.Select(p => p.Name));

        var fkUsuario = Assert.Single(entidad.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(Usuario));
        Assert.Equal([nameof(Auditoria.EmpresaId), nameof(Auditoria.UsuarioId)], fkUsuario.Properties.Select(p => p.Name));
        Assert.False(fkUsuario.IsRequired);

        Assert.Equal("jsonb", entidad.FindProperty(nameof(Auditoria.Antes))!.GetColumnType());
        Assert.Equal("jsonb", entidad.FindProperty(nameof(Auditoria.Despues))!.GetColumnType());
        Assert.True(entidad.FindProperty(nameof(Auditoria.UsuarioId))!.IsNullable);
        Assert.True(entidad.FindProperty(nameof(Auditoria.EntidadId))!.IsNullable);
    }

    private static string[] Clave(IEntityType entidad) =>
        entidad.FindPrimaryKey()!.Properties.Select(p => p.Name).ToArray();

    private sealed class ContextoFijo : IContextoEmpresa
    {
        public Guid? EmpresaIdActual { get; private set; }

        public void Establecer(Guid empresaId) => EmpresaIdActual = empresaId;
    }
}
