using SupportAtos.Api.Authentication;
using SupportAtos.Application.Abstractions;

namespace SupportAtos.Api.Providers;

/// <summary>
/// Lê o <see cref="PortalUserContext"/> já resolvido no HttpContext.Items:
/// pelo JwtBearer (equipe, OnTokenValidated) ou pelo middleware do portal
/// (API key + headers X-Portal-User-Id/X-Portal-Group-Id).
/// </summary>
public class HttpContextCurrentUserProvider : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private PortalUserContext? ContextoDisponivel()
        => _httpContextAccessor.HttpContext?.Items[PortalUserContextItems.Chave] as PortalUserContext;

    public bool IsAutenticado => ContextoDisponivel() is not null;

    public PortalUserContext UsuarioAtual
        => ContextoDisponivel()
            ?? throw new InvalidOperationException("Requisição sem usuário autenticado/provisionado.");
}
