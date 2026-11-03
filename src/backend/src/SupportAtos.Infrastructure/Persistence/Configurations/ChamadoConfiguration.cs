using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class ChamadoConfiguration : IEntityTypeConfiguration<Chamado>
{
    public void Configure(EntityTypeBuilder<Chamado> builder)
    {
        builder.ToTable("chamado");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.CodigoPublico)
            .HasColumnName("codigo_publico")
            .HasColumnType("varchar(20)");

        builder.HasIndex(c => c.CodigoPublico)
            .IsUnique();

        builder.Property(c => c.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(c => c.GrupoEmpresaId)
            .HasColumnName("grupo_empresa_id");

        builder.Property(c => c.AgenteId)
            .HasColumnName("agente_id");

        builder.Property(c => c.SlaCategoriaId)
            .HasColumnName("sla_config_id");

        builder.Property(c => c.Produto)
            .HasColumnName("produto")
            .HasColumnType("varchar(100)");

        builder.Property(c => c.Categoria)
            .HasColumnName("categoria")
            .HasColumnType("varchar(100)");

        builder.Property(c => c.Titulo)
            .HasColumnName("titulo")
            .HasColumnType("varchar(200)");

        builder.Property(c => c.Descricao)
            .HasColumnName("descricao")
            .HasColumnType("text");

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasConversion<string>();

        builder.Property(c => c.Prioridade)
            .HasColumnName("prioridade")
            .HasColumnType("varchar(20)")
            .HasConversion<string>();

        builder.Property(c => c.SlaPrazo)
            .HasColumnName("sla_prazo_resolucao")
            .HasColumnType("timestamp");

        builder.Property(c => c.SlaPrazoResposta)
            .HasColumnName("sla_prazo_resposta")
            .HasColumnType("timestamp");

        builder.Property(c => c.AguardandoDesde)
            .HasColumnName("aguardando_desde")
            .HasColumnType("timestamp");

        builder.Property(c => c.CriadoEm)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp");

        builder.Property(c => c.AtualizadoEm)
            .HasColumnName("atualizado_em")
            .HasColumnType("timestamp");

        builder.Property(c => c.FechadoEm)
            .HasColumnName("fechado_em")
            .HasColumnType("timestamp");

        builder.HasIndex(c => c.UsuarioId);
        builder.HasIndex(c => c.GrupoEmpresaId);
        builder.HasIndex(c => c.AgenteId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.SlaPrazo);
        builder.HasIndex(c => new { c.Produto, c.Categoria, c.Status, c.GrupoEmpresaId, c.CriadoEm });

        builder.HasOne(c => c.Usuario)
            .WithMany(u => u.Chamados)
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.GrupoEmpresa)
            .WithMany(g => g.Chamados)
            .HasForeignKey(c => c.GrupoEmpresaId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.Agente)
            .WithMany()
            .HasForeignKey(c => c.AgenteId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(c => c.SlaCategoria)
            .WithMany(s => s.Chamados)
            .HasForeignKey(c => c.SlaCategoriaId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
