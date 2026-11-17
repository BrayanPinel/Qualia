using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Persistence.Configurations;

internal sealed class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> b)
    {
        b.ToTable("empresa", t =>
        {
            t.HasCheckConstraint("ck_empresa_estado", Sql.EnEnum<EstadoEmpresa>("estado"));
            t.HasCheckConstraint("ck_empresa_minimo_muestra", "minimo_muestra > 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MinimoMuestra).HasDefaultValue(5);
        b.HasIndex(x => x.Codigo).IsUnique();
    }
}

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuario", t => t.HasCheckConstraint("ck_usuario_rol", Sql.EnEnum<RolEmpresa>("rol")));
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Correo).HasMaxLength(254).IsRequired();
        b.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => new { x.EmpresaId, x.Correo }).IsUnique();
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

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
