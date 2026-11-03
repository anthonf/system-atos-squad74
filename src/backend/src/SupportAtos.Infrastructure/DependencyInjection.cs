using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.Ports;
using SupportAtos.Infrastructure.Persistence;
using SupportAtos.Infrastructure.Repositories;

namespace SupportAtos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não está configurada.");

        services.AddDbContext<SupportAtosDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IGrupoEmpresaRepository, GrupoEmpresaRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IChamadoRepository, ChamadoRepository>();
        services.AddScoped<ISlaCategoriaRepository, SlaCategoriaRepository>();
        services.AddScoped<IProvisionamentoAuditRepository, ProvisionamentoAuditRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUniqueConstraintViolationDetector, NpgsqlUniqueViolationDetector>();

        services.AddHostedService<DevelopmentDbSeeder>();

        return services;
    }
}