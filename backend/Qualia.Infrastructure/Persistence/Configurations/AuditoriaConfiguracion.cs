using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qualia.Domain.Entidades;

namespace Qualia.Infrastructure.Persistence.Configurations;

internal sealed class AuditoriaConfiguracion : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> b)
    {
        b.ToTable("auditoria");
        b.HasKey(x => new { x.EmpresaId, x.Id });
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Accion).HasMaxLength(100).IsRequired();
        b.Property(x => x.Entidad).HasMaxLength(100).IsRequired();
        b.Property(x => x.Antes).HasColumnType("jsonb");
        b.Property(x => x.Despues).HasColumnType("jsonb");
        b.HasIndex(x => new { x.EmpresaId, x.Fecha });
        b.HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        // FK compuesta opcional: impide apuntar a un usuario de otra empresa.
        b.HasOne<Usuario>().WithMany()
            .HasForeignKey(x => new { x.EmpresaId, x.UsuarioId })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
