using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plan", t =>
        {
            t.HasCheckConstraint(
                "ck_plan_valores_no_negativos",
                "precio_mensual >= 0 AND minutos_incluidos >= 0 AND precio_excedente >= 0");
            t.HasCheckConstraint(
                "ck_plan_retencion_positiva",
                "retencion_dias > 0");
            t.HasCheckConstraint(
                "ck_plan_limites_positivos",
                "(limite_supervisores IS NULL OR limite_supervisores > 0) AND (limite_rubricas IS NULL OR limite_rubricas > 0)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(Plan.LongitudMaximaNombre)
            .IsRequired();

        builder.Property(p => p.PrecioMensual)
            .HasColumnName("precio_mensual")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.MinutosIncluidos)
            .HasColumnName("minutos_incluidos")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.PrecioExcedente)
            .HasColumnName("precio_excedente")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.Activo)
            .HasColumnName("activo")
            .IsRequired();

        builder.Property(p => p.LimiteSupervisores)
            .HasColumnName("limite_supervisores");

        builder.Property(p => p.LimiteRubricas)
            .HasColumnName("limite_rubricas");

        builder.Property(p => p.RetencionDias)
            .HasColumnName("retencion_dias")
            .IsRequired();

        builder.HasIndex(p => p.Nombre)
            .IsUnique()
            .HasDatabaseName("uq_plan_nombre");

        builder.Navigation(p => p.Funciones)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Funciones)
            .WithOne()
            .HasForeignKey(f => f.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new
            {
                Id = PlanesCatalogo.BasicoId,
                Nombre = "Básico",
                PrecioMensual = 29.00m,
                MinutosIncluidos = 1000.00m,
                PrecioExcedente = 0.05m,
                Activo = true,
                LimiteSupervisores = (int?)2,
                LimiteRubricas = (int?)1,
                RetencionDias = PlanesCatalogo.RetencionDiasMvp,
            },
            new
            {
                Id = PlanesCatalogo.ProId,
                Nombre = "Pro",
                PrecioMensual = 99.00m,
                MinutosIncluidos = 5000.00m,
                PrecioExcedente = 0.04m,
                Activo = true,
                LimiteSupervisores = (int?)10,
                LimiteRubricas = (int?)null,
                RetencionDias = PlanesCatalogo.RetencionDiasMvp,
            },
            new
            {
                Id = PlanesCatalogo.EnterpriseId,
                Nombre = "Enterprise",
                PrecioMensual = 299.00m,
                MinutosIncluidos = 20000.00m,
                PrecioExcedente = 0.03m,
                Activo = true,
                LimiteSupervisores = (int?)null,
                LimiteRubricas = (int?)null,
                RetencionDias = PlanesCatalogo.RetencionDiasMvp,
            });
    }
}
