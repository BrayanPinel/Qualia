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
