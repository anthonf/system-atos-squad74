using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence.Configurations;

public class ProvisionamentoAuditConfiguration : IEntityTypeConfiguration<ProvisionamentoAudit>
{
    public void Configure(EntityTypeBuilder<ProvisionamentoAudit> builder)
    {
        builder.ToTable("provisionamento_audit");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.CodigoExternoUsuario)
            .HasColumnName("codigo_externo_usuario")
            .HasColumnType("varchar");

        builder.Property(p => p.CodigoExternoGrupo)
            .HasColumnName("codigo_externo_grupo")
            .HasColumnType("varchar");

        builder.Property(p => p.Acao)
            .HasColumnName("acao")
            .HasColumnType("varchar");

        builder.Property(p => p.Data)
            .HasColumnName("data")
            .HasColumnType("timestamp");
    }
}