using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;
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

        // Verifica falla cerrada (EmpresaId == null -> TieneEmpresaActual = False) y aislamiento dinámico por empresa.
        var sqlSinEmpresa = db.Suscripciones.ToQueryString();
        Assert.Contains("TieneEmpresaActual", sqlSinEmpresa, StringComparison.Ordinal);
        Assert.Contains("='False'", sqlSinEmpresa, StringComparison.Ordinal);
        Assert.Contains("s.empresa_id =", sqlSinEmpresa, StringComparison.OrdinalIgnoreCase);

        var empresaId = Guid.NewGuid();
        contexto.EmpresaId = empresaId;
        var sqlConEmpresa = db.Suscripciones.ToQueryString();
        Assert.Contains("='True'", sqlConEmpresa, StringComparison.Ordinal);
        Assert.Contains(empresaId.ToString(), sqlConEmpresa, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ContextoMutable : IContextoEmpresa
    {
        public Guid? EmpresaId { get; set; }
    }
}
