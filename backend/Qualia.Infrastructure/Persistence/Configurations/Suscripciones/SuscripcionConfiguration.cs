using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class SuscripcionConfiguration : IEntityTypeConfiguration<Suscripcion>
{
    public void Configure(EntityTypeBuilder<Suscripcion> builder)
    {
        builder.ToTable("suscripcion", t =>
        {
            t.HasCheckConstraint(
                "ck_suscripcion_estado",
                "estado IN ('Prueba', 'Activa', 'EnMora', 'Vencida')");
            t.HasCheckConstraint(
                "ck_suscripcion_fechas",
                "fecha_vencimiento > fecha_inicio");
            t.HasCheckConstraint(
                "ck_suscripcion_gracia",
                "gracia_hasta IS NULL OR gracia_hasta >= fecha_vencimiento");
        });

        builder.HasKey(s => new { s.EmpresaId, s.Id });

        builder.Property(s => s.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.Property(s => s.FechaInicio)
            .HasColumnName("fecha_inicio")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(s => s.FechaVencimiento)
            .HasColumnName("fecha_vencimiento")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(s => s.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(Suscripcion.LongitudMaximaEstado)
            .IsRequired();

        builder.Property(s => s.PermiteExcedente)
            .HasColumnName("permite_excedente")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(s => s.GraciaHasta)
            .HasColumnName("gracia_hasta")
            .HasColumnType("timestamptz");

        builder.HasOne<Empresa>().WithMany().HasForeignKey(s => s.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.EmpresaId)
            .IsUnique()
            .HasDatabaseName("uq_suscripcion_vigente_empresa")
            .HasFilter("estado IN ('Prueba', 'Activa', 'EnMora')");
    }
}
