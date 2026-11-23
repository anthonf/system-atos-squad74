using Microsoft.EntityFrameworkCore;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class ChamadoRepository : IChamadoRepository
{
    private readonly SupportAtosDbContext _context;

    public ChamadoRepository(SupportAtosDbContext context)
    {
        _context = context;
    }

    public Task<Chamado?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Chamados.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Chamado?> GetByCodigoPublicoAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => _context.Chamados.FirstOrDefaultAsync(c => c.CodigoPublico == codigoPublico, cancellationToken);

    public Task<Chamado?> GetByCodigoPublicoComHistoricoAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => _context.Chamados
            .Include(c => c.Usuario)
            .Include(c => c.GrupoEmpresa)
            .Include(c => c.Agente)
            .Include(c => c.Interacoes)
                .ThenInclude(i => i.Anexos)
            .Include(c => c.Interacoes)
                .ThenInclude(i => i.Usuario)
            .FirstOrDefaultAsync(c => c.CodigoPublico == codigoPublico, cancellationToken);

    public Task<bool> CodigoPublicoExisteAsync(string codigoPublico, CancellationToken cancellationToken = default)
        => _context.Chamados.AnyAsync(c => c.CodigoPublico == codigoPublico, cancellationToken);

    public async Task AddAsync(Chamado chamado, CancellationToken cancellationToken = default)
        => await _context.Chamados.AddAsync(chamado, cancellationToken);

    public async Task<IReadOnlyList<Chamado>> ListarFilaAsync(
        FilaChamadosQuery filtro,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Chamados
            .AsNoTracking()
            .Include(c => c.Usuario)
            .Include(c => c.GrupoEmpresa)
            .Include(c => c.Agente)
            .AsQueryable();

        if (filtro.Status.HasValue)
        {
            query = query.Where(c => c.Status == filtro.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            query = query.Where(c => c.Categoria == filtro.Categoria);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Produto))
        {
            query = query.Where(c => c.Produto == filtro.Produto);
        }

        if (filtro.AgenteId.HasValue)
        {
            query = query.Where(c => c.AgenteId == filtro.AgenteId.Value);
        }

        if (filtro.GrupoEmpresaId.HasValue)
        {
            query = query.Where(c => c.GrupoEmpresaId == filtro.GrupoEmpresaId.Value);
        }

        if (filtro.SemAgente == true)
        {
            query = query.Where(c => c.AgenteId == null);
        }

        return await query
            .OrderBy(c => c.SlaPrazo)
            .ThenBy(c => c.CriadoEm)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarAbertosPorAgenteAsync(int agenteId, CancellationToken cancellationToken = default)
        => _context.Chamados.CountAsync(
            c => c.AgenteId == agenteId && (c.Status == StatusChamado.Aberto || c.Status == StatusChamado.EmAndamento),
            cancellationToken);
}
