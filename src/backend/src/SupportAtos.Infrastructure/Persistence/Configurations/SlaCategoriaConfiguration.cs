using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class SlaCategoriaConfiguration : IEntityTypeConfiguration<SlaCategoria>
{
    public void Configure(EntityTypeBuilder<SlaCategoria> builder)
    {
        // Tabela oficial: sla_config (SQL enviado). Nome da classe mantido para compat.
        builder.ToTable("sla_config");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.Produto)
            .HasColumnName("produto")
            .HasColumnType("varchar(100)");

        builder.Property(s => s.Categoria)
            .HasColumnName("categoria")
            .HasColumnType("varchar(100)");

        builder.Property(s => s.Prioridade)
            .HasColumnName("prioridade")
            .HasColumnType("varchar(20)")
            .HasConversion<string>();

        builder.Property(s => s.TempoResposta)
            .HasColumnName("tempo_resposta_min");

        builder.Property(s => s.TempoResolucao)
            .HasColumnName("tempo_resolucao_min");

        builder.Property(s => s.Ativo)
            .HasColumnName("ativo")
            .HasDefaultValue(true);

        builder.HasIndex(s => new { s.Produto, s.Categoria, s.Prioridade })
            .IsUnique();
    }
}
