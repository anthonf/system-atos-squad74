using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Application.Ports;
using SupportAtos.Application.Services;
using SupportAtos.Application.UseCases;

namespace SupportAtos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<CriarChamadoUseCase>();
        services.AddScoped<ConsultarChamadoUseCase>();
        services.AddScoped<ListarChamadosUseCase>();
        services.AddScoped<ResponderChamadoUseCase>();
        services.AddScoped<MudarStatusChamadoUseCase>();
        services.AddScoped<AtribuirAgenteUseCase>();
        services.AddScoped<CadastrarUsuarioUseCase>();
        services.AddScoped<ConsultarUsuarioUseCase>();
        services.AddScoped<ListarUsuariosPorGrupoUseCase>();

        return services;
    }
}
