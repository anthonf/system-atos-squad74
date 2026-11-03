using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class InteracaoConfiguration : IEntityTypeConfiguration<Interacao>
{
    public void Configure(EntityTypeBuilder<Interacao> builder)
    {
        builder.ToTable("interacao");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        // Coluna oficial: autor_id (SQL enviado).
        builder.Property(i => i.UsuarioId)
            .HasColumnName("autor_id");

        builder.Property(i => i.ChamadoId)
            .HasColumnName("chamado_id");

        builder.Property(i => i.Tipo)
            .HasColumnName("tipo")
            .HasColumnType("varchar(20)")
            .HasConversion<string>();

        builder.Property(i => i.Mensagem)
            .HasColumnName("mensagem")
            .HasColumnType("text");

        builder.Property(i => i.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp");

        builder.HasIndex(i => i.ChamadoId);
        builder.HasIndex(i => i.UsuarioId);

        builder.HasOne(i => i.Usuario)
            .WithMany(u => u.Interacoes)
            .HasForeignKey(i => i.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(i => i.Chamado)
            .WithMany(c => c.Interacoes)
            .HasForeignKey(i => i.ChamadoId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
