using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;

namespace SupportAtos.Application.UseCases;

public class ListarSlaCategoriasUseCase(ISlaCategoriaRepository slaCategoriaRepository)
{
    public async Task<IReadOnlyList<SlaCategoriaDto>> Handle(CancellationToken cancellationToken = default)
    {
        var categorias = await slaCategoriaRepository.ListarAtivasAsync(cancellationToken);
        return categorias.Select(s => new SlaCategoriaDto(s.Produto, s.Categoria, s.Prioridade)).ToList();
    }
}
