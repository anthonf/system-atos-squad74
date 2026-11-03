using Microsoft.AspNetCore.Authorization;
using SupportAtos.Application.Abstractions;

namespace SupportAtos.Api.Authentication;

/// <summary>
/// Requisito aplicado a endpoints que exigem um usuário real do portal
/// (providenciado via JWT). Autenticação via API key não satisfaz o requisito.
/// </summary>
public class UsuarioPortalRequirement : IAuthorizationRequirement
{
}

public class UsuarioPortalAuthorizationHandler : AuthorizationHandler<UsuarioPortalRequirement>
{
    private readonly ICurrentUser _currentUser;

    public UsuarioPortalAuthorizationHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UsuarioPortalRequirement requirement)
    {
        if (_currentUser.IsAutenticado)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}