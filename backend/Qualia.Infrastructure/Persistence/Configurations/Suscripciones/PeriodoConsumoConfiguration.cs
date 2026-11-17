using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class PeriodoConsumoConfiguration : IEntityTypeConfiguration<PeriodoConsumo>
{
    public void Configure(EntityTypeBuilder<PeriodoConsumo> builder)
    {
        builder.ToTable("periodo_consumo", t =>
        {
            t.HasCheckConstraint(
                "ck_periodo_consumo_fechas",
                "fecha_fin > fecha_inicio");
            t.HasCheckConstraint(
                "ck_periodo_consumo_minutos",
                "minutos_contratados >= 0");
        });

        builder.HasKey(p => new { p.EmpresaId, p.Id });

        builder.Property(p => p.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.SuscripcionId)
            .HasColumnName("suscripcion_id")
            .IsRequired();

        builder.Property(p => p.FechaInicio)
            .HasColumnName("fecha_inicio")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.FechaFin)
            .HasColumnName("fecha_fin")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(p => p.MinutosContratados)
            .HasColumnName("minutos_contratados")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Ignore(p => p.Mes);

        builder.HasOne<Empresa>().WithMany().HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Suscripcion)
            .WithMany()
            .HasForeignKey(p => new { p.EmpresaId, p.SuscripcionId })
            .HasPrincipalKey(s => new { s.EmpresaId, s.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.EmpresaId, p.SuscripcionId })
            .HasDatabaseName("ix_periodo_consumo_empresa_suscripcion");

        builder.HasIndex(p => new { p.EmpresaId, p.FechaFin })
            .IsUnique()
            .HasDatabaseName("uq_periodo_consumo_empresa_fin");
    }
}
