using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class AnexoConfiguration : IEntityTypeConfiguration<Anexo>
{
    public void Configure(EntityTypeBuilder<Anexo> builder)
    {
        builder.ToTable("anexo");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.InteracaoId)
            .HasColumnName("interacao_id");

        builder.Property(a => a.ChamadoId)
            .HasColumnName("chamado_id");

        builder.Property(a => a.NomeArquivo)
            .HasColumnName("nome_arquivo")
            .HasColumnType("varchar(255)");

        // Coluna oficial: caminho (SQL enviado).
        builder.Property(a => a.Url)
            .HasColumnName("caminho")
            .HasColumnType("varchar(500)");

        builder.Property(a => a.TamanhoBytes)
            .HasColumnName("tamanho_bytes");

        builder.Property(a => a.TipoMime)
            .HasColumnName("tipo_mime")
            .HasColumnType("varchar(100)");

        builder.HasIndex(a => a.InteracaoId);
        builder.HasIndex(a => a.ChamadoId);

        builder.HasOne(a => a.Interacao)
            .WithMany(i => i.Anexos)
            .HasForeignKey(a => a.InteracaoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(a => a.Chamado)
            .WithMany()
            .HasForeignKey(a => a.ChamadoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
