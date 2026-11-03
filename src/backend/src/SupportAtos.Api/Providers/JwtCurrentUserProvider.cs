using SupportAtos.Api.Authentication;
using SupportAtos.Application.Abstractions;

namespace SupportAtos.Api.Providers;

/// <summary>
/// Implementação real do usuário autenticado: lê o PortalUserContext já
/// provisionado no OnTokenValidated do JwtBearer (guardado no HttpContext.Items).
/// </summary>
public class JwtCurrentUserProvider : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private PortalUserContext? ContextoDisponivel()
        => _httpContextAccessor.HttpContext?.Items[PortalUserContextItems.Chave] as PortalUserContext;

    public bool IsAutenticado => ContextoDisponivel() is not null;

    public PortalUserContext UsuarioAtual
        => ContextoDisponivel()
            ?? throw new InvalidOperationException("Requisição sem usuário do portal autenticado/provisionado.");
}