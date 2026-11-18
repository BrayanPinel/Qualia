using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Persistence.Configurations;

internal sealed class DisputaConfiguracion : IEntityTypeConfiguration<Disputa>
{
    public void Configure(EntityTypeBuilder<Disputa> b)
    {
        b.ToTable("disputa", t =>
        {
            t.HasCheckConstraint("ck_disputa_estado", Sql.EnEnum<EstadoDisputa>("estado"));
            // Abierta: sin datos de resolución. Resuelta: resolución, resolutor y fecha obligatorios.
            t.HasCheckConstraint("ck_disputa_resolucion",
                "(estado = 'Abierta' AND resolucion IS NULL AND resuelta_por_id IS NULL AND resuelta_en IS NULL) OR " +
                "(estado = 'Resuelta' AND resolucion IS NOT NULL AND resuelta_por_id IS NOT NULL AND resuelta_en IS NOT NULL)");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Motivo).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(10);

        // Una sola disputa Abierta por evaluación (índice único parcial).
        b.HasIndex(x => new { x.EmpresaId, x.EvaluacionId })
            .IsUnique()
            .HasFilter("estado = 'Abierta'")
            .HasDatabaseName("ux_disputa_abierta_por_evaluacion");
        b.HasIndex(x => new { x.EmpresaId, x.Estado, x.CreadaEn });

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<EvaluacionIA>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.EvaluacionId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.ResueltaPorId })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
