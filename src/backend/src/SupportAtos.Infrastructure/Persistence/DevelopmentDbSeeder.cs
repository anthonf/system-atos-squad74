using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Infrastructure.Persistence;

public class DevelopmentDbSeeder : IHostedService
{
    private readonly IHostEnvironment _environment;
    private readonly IServiceScopeFactory _scopeFactory;

    public DevelopmentDbSeeder(IHostEnvironment environment, IServiceScopeFactory scopeFactory)
    {
        _environment = environment;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportAtosDbContext>();

        if (await dbContext.GrupoEmpresas.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.GrupoEmpresas.AddRange(
            GrupoEmpresa.Criar("Empresa Demo"),
            GrupoEmpresa.Criar("Empresa Teste"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}