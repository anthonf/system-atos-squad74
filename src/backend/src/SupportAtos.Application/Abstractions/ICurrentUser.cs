namespace SupportAtos.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAutenticado { get; }

    PortalUserContext UsuarioAtual { get; }
}