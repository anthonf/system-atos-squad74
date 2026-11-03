using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id");

        builder.Property(u => u.Nome)
            .HasColumnName("nome")
            .HasColumnType("varchar(150)");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasColumnType("varchar(255)");

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.GrupoEmpresaId);

        builder.Property(u => u.CodigoExterno)
            .HasColumnName("codigo_externo")
            .HasColumnType("varchar");

        builder.HasIndex(u => u.CodigoExterno)
            .IsUnique();

        builder.Property(u => u.GrupoEmpresaId)
            .HasColumnName("grupo_empresa_id");

        builder.Property(u => u.Papel)
            .HasColumnName("papel")
            .HasColumnType("varchar(20)")
            .HasConversion<string>();

        builder.Property(u => u.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp");

        builder.Property(u => u.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp");

        builder.HasOne(u => u.GrupoEmpresa)
            .WithMany(g => g.Usuarios)
            .HasForeignKey(u => u.GrupoEmpresaId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
