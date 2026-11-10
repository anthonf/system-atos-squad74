using SupportAtos.Api.Authentication;

namespace SupportAtos.Api.Middleware;

public class PortalUserContextMiddleware
{
    private readonly RequestDelegate _next;

    public PortalUserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, PortalUserResolver resolver)
    {
        var aberturaDeChamado = HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.Equals("/api/chamados", StringComparison.OrdinalIgnoreCase);

        if (!aberturaDeChamado
            && context.User.HasClaim(AuthenticationSetup.ScopeClaim, AuthenticationSetup.PortalScope))
        {
            context.Items[PortalUserContextItems.Chave] =
                await resolver.ResolverAsync(context, context.RequestAborted);
        }

        await _next(context);
    }
}
