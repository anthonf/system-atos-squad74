using SupportAtos.Application.Ports;
using SupportAtos.Application.UseCases;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

namespace SupportAtos.Application.Tests;

public class ListarSlaCategoriasUseCaseTests
{
    [Fact]
    public async Task Handle_DeveRetornarSomenteCategoriasAtivas()
    {
        var repositorio = new FakeSlaCategoriaRepository();
        repositorio.Adicionar(SlaCategoria.Criar("Cartões", "Faturamento", Prioridade.Media, 2, 8));
        repositorio.Adicionar(SlaCategoria.Criar("Portal", "Encerrado", Prioridade.Baixa, 1, 4, ativo: false));
        var useCase = new ListarSlaCategoriasUseCase(repositorio);

        var categorias = await useCase.Handle();

        Assert.Equal(2, categorias.Count);
        Assert.Contains(categorias, c => c.Produto == "Portal" && c.Categoria == "Login" && c.Prioridade == Prioridade.Alta);
        Assert.Contains(categorias, c => c.Produto == "Cartões" && c.Categoria == "Faturamento");
        Assert.DoesNotContain(categorias, c => c.Categoria == "Encerrado");
    }

    private sealed class FakeSlaCategoriaRepository : ISlaCategoriaRepository
    {
        private readonly List<SlaCategoria> _categorias = new()
        {
            SlaCategoria.Criar("Portal", "Login", Prioridade.Alta, 1, 4)
        };

        public void Adicionar(SlaCategoria categoria) => _categorias.Add(categoria);

        public Task<SlaCategoria?> GetByProdutoECategoriaAsync(string produto, string categoria, CancellationToken cancellationToken = default)
            => Task.FromResult(_categorias.FirstOrDefault(s => s.Produto == produto && s.Categoria == categoria));

        public Task<IReadOnlyList<SlaCategoria>> ListarAtivasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SlaCategoria>>(_categorias.Where(s => s.Ativo).ToList());
    }
}
