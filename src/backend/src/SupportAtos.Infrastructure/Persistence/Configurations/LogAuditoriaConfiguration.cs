using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class LogAuditoriaConfiguration : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.ToTable("log_auditoria");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.ChamadoId)
            .HasColumnName("chamado_id");

        builder.Property(l => l.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(l => l.Acao)
            .HasColumnName("acao")
            .HasColumnType("varchar(50)");

        builder.Property(l => l.CampoAlterado)
            .HasColumnName("campo_alterado")
            .HasColumnType("varchar(100)");

        builder.Property(l => l.ValorAnterior)
            .HasColumnName("valor_anterior")
            .HasColumnType("text");

        builder.Property(l => l.ValorNovo)
            .HasColumnName("valor_novo")
            .HasColumnType("text");

        // Coluna oficial: criado_em (SQL enviado).
        builder.Property(l => l.Data)
            .HasColumnName("criado_em")
            .HasColumnType("timestamp");

        builder.HasIndex(l => l.ChamadoId);
        builder.HasIndex(l => l.Data);
        builder.HasIndex(l => l.UsuarioId);

        builder.HasOne(l => l.Chamado)
            .WithMany(c => c.LogsAuditoria)
            .HasForeignKey(l => l.ChamadoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(l => l.Usuario)
            .WithMany(u => u.LogsAuditoria)
            .HasForeignKey(l => l.UsuarioId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
