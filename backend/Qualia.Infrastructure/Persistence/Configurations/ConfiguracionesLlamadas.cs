using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Persistence.Configurations;

internal sealed class LlamadaConfiguracion : IEntityTypeConfiguration<Llamada>
{
    public void Configure(EntityTypeBuilder<Llamada> b)
    {
        b.ToTable("llamada", t =>
        {
            t.HasCheckConstraint("ck_llamada_estado", Sql.EnEnum<EstadoLlamada>("estado"));
            t.HasCheckConstraint("ck_llamada_duracion", "duracion_seg > 0");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.ArchivoRuta).IsRequired();
        b.Property(x => x.DuracionSeg).HasPrecision(10, 2);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);

        // Índice de consulta requerido: Llamada(empresa_id, estado, rubrica_id).
        b.HasIndex(x => new { x.EmpresaId, x.Estado, x.RubricaId });

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Rubrica).WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.RubricaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Agente).WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.AgenteId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TrabajoProcesamientoConfiguracion : IEntityTypeConfiguration<TrabajoProcesamiento>
{
    public void Configure(EntityTypeBuilder<TrabajoProcesamiento> b)
    {
        b.ToTable("trabajo_procesamiento", t =>
        {
            t.HasCheckConstraint("ck_trabajo_procesamiento_estado", Sql.EnEnum<EstadoTrabajo>("estado"));
            t.HasCheckConstraint("ck_trabajo_procesamiento_intentos", "intentos >= 0");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(50);

        // Un solo trabajo por llamada: UNIQUE (empresa_id, llamada_id).
        b.HasOne(x => x.Llamada).WithOne(l => l.Trabajo)
            .HasForeignKey<TrabajoProcesamiento>(x => new { x.EmpresaId, x.LlamadaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EmpresaId, x.LlamadaId }).IsUnique();

        // Índice de la cola del worker: (estado, proximo_intento).
        b.HasIndex(x => new { x.Estado, x.ProximoIntento });

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SegmentoTranscripcionConfiguracion : IEntityTypeConfiguration<SegmentoTranscripcion>
{
    public void Configure(EntityTypeBuilder<SegmentoTranscripcion> b)
    {
        b.ToTable("segmento_transcripcion", t =>
        {
            t.HasCheckConstraint("ck_segmento_transcripcion_tiempos", "inicio_seg >= 0 AND fin_seg >= inicio_seg");
            t.HasCheckConstraint("ck_segmento_transcripcion_orden", "orden > 0");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Hablante).HasMaxLength(50).IsRequired();
        b.Property(x => x.InicioSeg).HasPrecision(10, 2);
        b.Property(x => x.FinSeg).HasPrecision(10, 2);
        b.Property(x => x.Texto).IsRequired();

        b.HasIndex(x => new { x.EmpresaId, x.LlamadaId, x.Orden }).IsUnique();

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Llamada>().WithMany(l => l.Segmentos)
            .HasForeignKey(x => new { x.EmpresaId, x.LlamadaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EvaluacionIAConfiguracion : IEntityTypeConfiguration<EvaluacionIA>
{
    public void Configure(EntityTypeBuilder<EvaluacionIA> b)
    {
        b.ToTable("evaluacion_ia", t =>
        {
            t.HasCheckConstraint("ck_evaluacion_ia_estado", Sql.EnEnum<EstadoEvaluacion>("estado"));
            t.HasCheckConstraint("ck_evaluacion_ia_modo", Sql.EnEnum<ModoIA>("modo_ia"));
            t.HasCheckConstraint("ck_evaluacion_ia_revision", "revision > 0");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.ModeloUsado).HasMaxLength(100).IsRequired();
        b.Property(x => x.ModoIA).HasColumnName("modo_ia").HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Revision).HasDefaultValue(1);

        // Una sola evaluación por llamada: UNIQUE (empresa_id, llamada_id).
        b.HasOne<Llamada>().WithOne(l => l.Evaluacion)
            .HasForeignKey<EvaluacionIA>(x => new { x.EmpresaId, x.LlamadaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EmpresaId, x.LlamadaId }).IsUnique();

        b.HasMany(x => x.Detalles).WithOne()
            .HasForeignKey(d => new { d.EmpresaId, d.EvaluacionId })
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DetalleEvaluacionConfiguracion : IEntityTypeConfiguration<DetalleEvaluacion>
{
    public void Configure(EntityTypeBuilder<DetalleEvaluacion> b)
    {
        b.ToTable("detalle_evaluacion", t =>
            t.HasCheckConstraint("ck_detalle_evaluacion_puntaje", "puntaje_criterio >= 0 AND puntaje_criterio <= 100"));
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.PuntajeCriterio).HasPrecision(5, 2);
        b.Property(x => x.Observacion).IsRequired();

        // Un resultado por criterio dentro de la evaluación.
        b.HasIndex(x => new { x.EmpresaId, x.EvaluacionId, x.CriterioId }).IsUnique();

        b.HasOne(x => x.Criterio).WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.CriterioId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Evidencias).WithOne()
            .HasForeignKey(e => new { e.EmpresaId, e.DetalleId })
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EvidenciaCriterioConfiguracion : IEntityTypeConfiguration<EvidenciaCriterio>
{
    public void Configure(EntityTypeBuilder<EvidenciaCriterio> b)
    {
        b.ToTable("evidencia_criterio");
        b.HasKey(x => new { x.EmpresaId, x.DetalleId, x.SegmentoId });
        b.Property(x => x.CitaTextual).IsRequired();

        b.HasOne(x => x.Segmento).WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.SegmentoId })
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
