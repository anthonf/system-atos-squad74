using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;

namespace SupportAtos.Application.UseCases;

public class ListarAgentesUseCase(IUsuarioRepository usuarioRepository)
{
    public async Task<IReadOnlyList<AgenteDto>> Handle(CancellationToken cancellationToken = default)
    {
        var agentes = await usuarioRepository.ListarAgentesAsync(cancellationToken);
        return agentes.Select(u => new AgenteDto(u.Id, u.Nome, u.Papel)).ToList();
    }
}
