using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Qualia.Application.Common.Interfaces;
using Qualia.Domain.Common;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence;

/// <summary>
/// Contexto central de Qualia. Descubre configuraciones <see cref="IEntityTypeConfiguration{TEntity}"/>
/// por ensamblado y aplica el filtro global multiempresa (falla cerrada cuando no hay empresa activa)
/// a toda entidad que implemente <see cref="IEntidadEmpresa"/>.
/// </summary>
public sealed class QualiaDbContext : DbContext
{
    private readonly IContextoEmpresa? _contextoEmpresa;

    public QualiaDbContext(
        DbContextOptions<QualiaDbContext> options,
        IContextoEmpresa? contextoEmpresa = null)
        : base(options)
    {
        _contextoEmpresa = contextoEmpresa;
    }

    public DbSet<Plan> Planes => Set<Plan>();

    public DbSet<FuncionPlan> FuncionesPlan => Set<FuncionPlan>();

    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();

    public DbSet<PeriodoConsumo> PeriodosConsumo => Set<PeriodoConsumo>();

    public DbSet<ConsumoLlamada> ConsumosLlamada => Set<ConsumoLlamada>();

    public DbSet<AuditoriaPlataforma> AuditoriasPlataforma => Set<AuditoriaPlataforma>();

    private bool TieneEmpresaActual =>
        _contextoEmpresa?.EmpresaId is { } empresaId && empresaId != Guid.Empty;

    private Guid EmpresaIdActual => _contextoEmpresa?.EmpresaId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cada rol agrega IEntityTypeConfiguration<T> en Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QualiaDbContext).Assembly);

        AplicarFiltrosEmpresa(modelBuilder);
    }

    private void AplicarFiltrosEmpresa(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IEntidadEmpresa).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var metodo = typeof(QualiaDbContext)
                .GetMethod(nameof(ConfigurarFiltroEmpresa), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType);

            metodo.Invoke(this, [modelBuilder]);
        }
    }

    private void ConfigurarFiltroEmpresa<TEntidad>(ModelBuilder modelBuilder)
        where TEntidad : class, IEntidadEmpresa
    {
        modelBuilder.Entity<TEntidad>().HasQueryFilter(e =>
            TieneEmpresaActual && e.EmpresaId == EmpresaIdActual);
    }
}
