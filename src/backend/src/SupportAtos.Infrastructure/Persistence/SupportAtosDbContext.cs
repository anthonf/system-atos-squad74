using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence;

public class SupportAtosDbContext : DbContext
{
    private static readonly ValueConverter<DateTime, DateTime> TimestampSemFusoConverter = new(
        v => v.Kind == DateTimeKind.Utc ? DateTime.SpecifyKind(v, DateTimeKind.Unspecified) : v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> TimestampNullableSemFusoConverter = new(
        v => v.HasValue && v.Value.Kind == DateTimeKind.Utc
            ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified)
            : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public SupportAtosDbContext(DbContextOptions<SupportAtosDbContext> options)
        : base(options)
    {
    }

    public DbSet<GrupoEmpresa> GrupoEmpresas => Set<GrupoEmpresa>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<SlaCategoria> SlaCategorias => Set<SlaCategoria>();

    public DbSet<Chamado> Chamados => Set<Chamado>();

    public DbSet<Interacao> Interacoes => Set<Interacao>();

    public DbSet<Anexo> Anexos => Set<Anexo>();

    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    public DbSet<ProvisionamentoAudit> ProvisionamentoAudits => Set<ProvisionamentoAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupportAtosDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(TimestampSemFusoConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(TimestampNullableSemFusoConverter);
                }
            }
        }
    }
}