using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Suscripciones;

namespace Qualia.Infrastructure.Persistence.Configurations.Suscripciones;

public sealed class AuditoriaPlataformaConfiguration : IEntityTypeConfiguration<AuditoriaPlataforma>
{
    public void Configure(EntityTypeBuilder<AuditoriaPlataforma> builder)
    {
        builder.ToTable("auditoria_plataforma");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(a => a.AdministradorId)
            .HasColumnName("administrador_id")
            .IsRequired();

        builder.Property(a => a.EmpresaObjetivoId)
            .HasColumnName("empresa_objetivo_id")
            .IsRequired();

        builder.Property(a => a.Accion)
            .HasColumnName("accion")
            .HasMaxLength(AuditoriaPlataforma.LongitudMaximaAccion)
            .IsRequired();

        builder.Property(a => a.Antes)
            .HasColumnName("antes")
            .HasColumnType("jsonb");

        builder.Property(a => a.Despues)
            .HasColumnName("despues")
            .HasColumnType("jsonb");

        builder.Property(a => a.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasOne<Empresa>().WithMany().HasForeignKey(a => a.EmpresaObjetivoId).OnDelete(DeleteBehavior.Restrict);

        // TODO (Integración Rol A): agregar FK a administrador(id) cuando exista la entidad Administrador.
        builder.HasIndex(a => new { a.EmpresaObjetivoId, a.Fecha })
            .HasDatabaseName("ix_auditoria_plataforma_empresa_fecha");

        builder.HasIndex(a => new { a.AdministradorId, a.Fecha })
            .HasDatabaseName("ix_auditoria_plataforma_admin_fecha");
    }
}
