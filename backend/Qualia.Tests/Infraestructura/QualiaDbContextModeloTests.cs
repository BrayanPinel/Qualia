using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Seguridad;
using Qualia.Domain.Suscripciones;
using Qualia.Infrastructure.Persistence;

namespace Qualia.Tests.Infraestructura;

public sealed class QualiaDbContextModeloTests
{
    [Fact]
    public void ModeloConfiguraTablasClavesCompuestasYFiltroMultiempresa()
    {
        var options = new DbContextOptionsBuilder<QualiaDbContext>()
            .UseNpgsql("Host=localhost;Database=QualiaTest;Username=postgres;Password=postgres")
            .Options;

        var contexto = new ContextoMutable();
        using var db = new QualiaDbContext(options, contexto);

        var suscripcionEntity = db.Model.FindEntityType(typeof(Suscripcion))!;
        Assert.Equal("suscripcion", suscripcionEntity.GetTableName());
        Assert.Equal(["EmpresaId", "Id"], suscripcionEntity.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.NotEmpty(suscripcionEntity.GetDeclaredQueryFilters());

        var indiceVigente = suscripcionEntity.GetIndexes()
            .Single(i => i.GetDatabaseName() == "uq_suscripcion_vigente_empresa");
        Assert.True(indiceVigente.IsUnique);
        Assert.Equal("estado IN ('Prueba', 'Activa', 'EnMora')", indiceVigente.GetFilter());

        var periodoEntity = db.Model.FindEntityType(typeof(PeriodoConsumo))!;
        Assert.Equal("periodo_consumo", periodoEntity.GetTableName());
        Assert.NotEmpty(periodoEntity.GetDeclaredQueryFilters());

        var consumoEntity = db.Model.FindEntityType(typeof(ConsumoLlamada))!;
        Assert.Equal("consumo_llamada", consumoEntity.GetTableName());
        Assert.NotEmpty(consumoEntity.GetDeclaredQueryFilters());

        // Tablas globales no llevan filtro multiempresa.
        var planEntity = db.Model.FindEntityType(typeof(Plan))!;
        Assert.Equal("plan", planEntity.GetTableName());
        Assert.Empty(planEntity.GetDeclaredQueryFilters());

        var auditoriaEntity = db.Model.FindEntityType(typeof(AuditoriaPlataforma))!;
        Assert.Equal("auditoria_plataforma", auditoriaEntity.GetTableName());
        Assert.Empty(auditoriaEntity.GetDeclaredQueryFilters());

        // Falla cerrada: sin empresa activa, la consulta igual lleva el filtro por empresa_id.
        var sqlSinEmpresa = db.Suscripciones.ToQueryString();
        Assert.Contains("empresa_id", sqlSinEmpresa, StringComparison.OrdinalIgnoreCase);

        // Con empresa activa, el filtro usa su identificador.
        var empresaId = Guid.NewGuid();
        contexto.EmpresaIdActual = empresaId;
        var sqlConEmpresa = db.Suscripciones.ToQueryString();
        Assert.Contains(empresaId.ToString(), sqlConEmpresa, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ContextoMutable : IContextoEmpresa
    {
        public Guid? EmpresaIdActual { get; set; }

        public void Establecer(Guid empresaId) => EmpresaIdActual = empresaId;
    }
}
