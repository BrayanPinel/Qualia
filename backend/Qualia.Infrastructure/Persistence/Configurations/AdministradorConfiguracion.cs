using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;

namespace Qualia.Infrastructure.Persistence.Configurations;

internal sealed class AdministradorConfiguracion : IEntityTypeConfiguration<Administrador>
{
    public void Configure(EntityTypeBuilder<Administrador> b)
    {
        b.ToTable("administrador");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Correo).HasMaxLength(254).IsRequired();
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => x.Correo).IsUnique();
    }
}
