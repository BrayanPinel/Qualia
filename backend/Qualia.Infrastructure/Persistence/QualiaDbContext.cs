using Microsoft.EntityFrameworkCore;

namespace Qualia.Infrastructure.Persistence;

/// <summary>
/// Contexto central de Qualia. En A00 queda vacío a propósito:
/// las entidades y sus configuraciones se agregan en A01 y en las
/// tareas de los roles B/C/D. No se generan migraciones sin coordinación.
/// </summary>
public sealed class QualiaDbContext : DbContext
{
    public QualiaDbContext(DbContextOptions<QualiaDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cada rol agrega IEntityTypeConfiguration<T> en Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QualiaDbContext).Assembly);
    }
}
