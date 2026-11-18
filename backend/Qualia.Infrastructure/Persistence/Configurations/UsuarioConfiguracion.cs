using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;
using Qualia.Domain.Enums;

namespace Qualia.Infrastructure.Persistence.Configurations;

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
