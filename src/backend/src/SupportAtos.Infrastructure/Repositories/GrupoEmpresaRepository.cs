using Microsoft.EntityFrameworkCore;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class GrupoEmpresaRepository : IGrupoEmpresaRepository
{
    private readonly SupportAtosDbContext _context;

    public GrupoEmpresaRepository(SupportAtosDbContext context)
    {
        _context = context;
    }

    public Task<GrupoEmpresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.GrupoEmpresas.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public Task<GrupoEmpresa?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default)
        => _context.GrupoEmpresas.FirstOrDefaultAsync(g => g.CodigoExterno == codigoExterno, cancellationToken);

    public async Task AddAsync(GrupoEmpresa grupoEmpresa, CancellationToken cancellationToken = default)
        => await _context.GrupoEmpresas.AddAsync(grupoEmpresa, cancellationToken);
}