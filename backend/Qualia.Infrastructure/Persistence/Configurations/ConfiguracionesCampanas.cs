using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Persistence.Configurations;

// Rol B — Campañas, asignaciones, rúbricas versionadas y revisión humana.

internal sealed class CampanaConfiguracion : IEntityTypeConfiguration<Campana>
{
    public void Configure(EntityTypeBuilder<Campana> b)
    {
        b.ToTable("campana");
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsuarioCampanaConfiguracion : IEntityTypeConfiguration<UsuarioCampana>
{
    public void Configure(EntityTypeBuilder<UsuarioCampana> b)
    {
        b.ToTable("usuario_campana");
        b.HasKey(x => new { x.EmpresaId, x.UsuarioId, x.CampanaId });
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.UsuarioId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Campana>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.CampanaId }).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RubricaConfiguracion : IEntityTypeConfiguration<Rubrica>
{
    public void Configure(EntityTypeBuilder<Rubrica> b)
    {
        b.ToTable("rubrica", t =>
        {
            t.HasCheckConstraint("ck_rubrica_estado", Sql.EnEnum<EstadoRubrica>("estado"));
            t.HasCheckConstraint("ck_rubrica_version", "version > 0");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.EstaPublicada);
        b.HasIndex(x => new { x.EmpresaId, x.CampanaId, x.Version }).IsUnique();
        // Una sola versión Activa por campaña (índice único parcial).
        b.HasIndex(x => new { x.EmpresaId, x.CampanaId })
            .IsUnique()
            .HasFilter("estado = 'Activa'")
            .HasDatabaseName("ux_rubrica_activa_por_campana");
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Campana>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.CampanaId }).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Criterios).WithOne()
            .HasForeignKey(c => new { c.EmpresaId, c.RubricaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CriterioRubricaConfiguracion : IEntityTypeConfiguration<CriterioRubrica>
{
    public void Configure(EntityTypeBuilder<CriterioRubrica> b)
    {
        b.ToTable("criterio_rubrica", t =>
        {
            t.HasCheckConstraint("ck_criterio_rubrica_peso", "peso > 0 AND peso <= 100");
            t.HasCheckConstraint("ck_criterio_rubrica_umbral", "umbral_cumplimiento >= 0 AND umbral_cumplimiento <= 100");
        });
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Descripcion).IsRequired();
        b.Property(x => x.Peso).HasPrecision(5, 2);
        b.Property(x => x.UmbralCumplimiento).HasPrecision(5, 2);
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RevisionHumanaConfiguracion : IEntityTypeConfiguration<RevisionHumana>
{
    public void Configure(EntityTypeBuilder<RevisionHumana> b)
    {
        b.ToTable("revision_humana");
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Comentarios).IsRequired();
        b.Ignore(x => x.EsAjuste);

        // Índice de consulta requerido: Revision_humana(empresa_id, evaluacion_id, fecha_revision).
        b.HasIndex(x => new { x.EmpresaId, x.EvaluacionId, x.FechaRevision });

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<EvaluacionIA>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.EvaluacionId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.SupervisorId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Criterios).WithOne()
            .HasForeignKey(c => new { c.EmpresaId, c.RevisionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CriterioRevisadoConfiguracion : IEntityTypeConfiguration<CriterioRevisado>
{
    public void Configure(EntityTypeBuilder<CriterioRevisado> b)
    {
        b.ToTable("criterio_revisado", t =>
            t.HasCheckConstraint("ck_criterio_revisado_puntaje", "puntaje_ajustado >= 0 AND puntaje_ajustado <= 100"));
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.PuntajeAjustado).HasPrecision(5, 2);
        b.Property(x => x.Observacion).IsRequired();

        // Un ajuste por detalle dentro de cada revisión.
        b.HasIndex(x => new { x.EmpresaId, x.RevisionId, x.DetalleId }).IsUnique();

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DetalleEvaluacion>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.DetalleId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Evidencias).WithOne()
            .HasForeignKey(e => new { e.EmpresaId, e.CriterioRevisadoId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EvidenciaRevisionConfiguracion : IEntityTypeConfiguration<EvidenciaRevision>
{
    public void Configure(EntityTypeBuilder<EvidenciaRevision> b)
    {
        b.ToTable("evidencia_revision");
        b.HasKey(x => new { x.EmpresaId, x.CriterioRevisadoId, x.SegmentoId });
        b.Property(x => x.CitaTextual).IsRequired();

        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SegmentoTranscripcion>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.SegmentoId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
