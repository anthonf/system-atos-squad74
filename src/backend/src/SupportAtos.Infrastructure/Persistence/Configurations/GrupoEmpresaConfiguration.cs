using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class GrupoEmpresaConfiguration : IEntityTypeConfiguration<GrupoEmpresa>
{
    public void Configure(EntityTypeBuilder<GrupoEmpresa> builder)
    {
        builder.ToTable("grupo_empresa");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id");

        builder.Property(g => g.Nome)
            .HasColumnName("nome")
            .HasColumnType("varchar(150)");

        builder.Property(g => g.Tipo)
            .HasColumnName("tipo")
            .HasColumnType("varchar(20)")
            .HasDefaultValue("matriz");

        builder.Property(g => g.GrupoPaiId)
            .HasColumnName("grupo_pai_id");

        builder.Property(g => g.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp");

        builder.Property(g => g.CodigoExterno)
            .HasColumnName("codigo_externo")
            .HasColumnType("varchar");

        builder.HasIndex(g => g.CodigoExterno)
            .IsUnique();

        builder.HasIndex(g => g.GrupoPaiId);

        builder.HasOne(g => g.GrupoPai)
            .WithMany()
            .HasForeignKey(g => g.GrupoPaiId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
