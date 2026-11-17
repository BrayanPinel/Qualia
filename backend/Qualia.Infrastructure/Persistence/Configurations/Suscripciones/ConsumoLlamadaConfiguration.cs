using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class ConsumoLlamadaConfiguration : IEntityTypeConfiguration<ConsumoLlamada>
{
    public void Configure(EntityTypeBuilder<ConsumoLlamada> builder)
    {
        builder.ToTable("consumo_llamada", t =>
        {
            t.HasCheckConstraint(
                "ck_consumo_llamada_estado",
                "estado IN ('Reservado', 'Facturado', 'Liberado')");
            t.HasCheckConstraint(
                "ck_consumo_llamada_valores",
                "minutos_facturables >= 0 AND minutos_reservados >= 0 AND tokens_transcriptor >= 0 AND tokens_entrada_evaluador >= 0 AND tokens_salida_evaluador >= 0 AND costo_estimado >= 0");
        });

        builder.HasKey(c => new { c.EmpresaId, c.Id });

        builder.Property(c => c.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.LlamadaId)
            .HasColumnName("llamada_id")
            .IsRequired();

        builder.Property(c => c.PeriodoId)
            .HasColumnName("periodo_id")
            .IsRequired();

        builder.Property(c => c.MinutosFacturables)
            .HasColumnName("minutos_facturables")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(c => c.TokensTranscriptor)
            .HasColumnName("tokens_transcriptor")
            .IsRequired();

        builder.Property(c => c.TokensEntradaEvaluador)
            .HasColumnName("tokens_entrada_evaluador")
            .IsRequired();

        builder.Property(c => c.TokensSalidaEvaluador)
            .HasColumnName("tokens_salida_evaluador")
            .IsRequired();

        builder.Property(c => c.CostoEstimado)
            .HasColumnName("costo_estimado")
            .HasPrecision(12, 6)
            .IsRequired();

        builder.Property(c => c.MinutosReservados)
            .HasColumnName("minutos_reservados")
            .HasPrecision(12, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(ConsumoLlamada.LongitudMaximaEstado)
            .IsRequired();

        // TODO (Integración Rol A/D): agregar FKs a empresa(id) y llamada(empresa_id, id) cuando existan.
        builder.HasOne(c => c.Periodo)
            .WithMany()
            .HasForeignKey(c => new { c.EmpresaId, c.PeriodoId })
            .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.EmpresaId, c.LlamadaId })
            .IsUnique()
            .HasDatabaseName("uq_consumo_llamada_empresa_llamada");

        builder.HasIndex(c => new { c.EmpresaId, c.PeriodoId })
            .HasDatabaseName("ix_consumo_llamada_empresa_periodo");
    }
}
