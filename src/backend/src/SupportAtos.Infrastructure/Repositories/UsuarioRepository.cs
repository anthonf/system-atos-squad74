using Microsoft.EntityFrameworkCore;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Infrastructure.Persistence;

namespace SupportAtos.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly SupportAtosDbContext _context;

    public UsuarioRepository(SupportAtosDbContext context)
    {
        _context = context;
    }

    public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Usuario?> GetComGrupoEmpresaPorIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Usuarios
            .Include(u => u.GrupoEmpresa)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ListarPorGrupoEmpresaAsync(int grupoEmpresaId, CancellationToken cancellationToken = default)
        => await _context.Usuarios
            .Include(u => u.GrupoEmpresa)
            .Where(u => u.GrupoEmpresaId == grupoEmpresaId)
            .OrderBy(u => u.Nome)
            .ToListAsync(cancellationToken);

    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> GetByCodigoExternoAsync(string codigoExterno, CancellationToken cancellationToken = default)
        => _context.Usuarios.FirstOrDefaultAsync(u => u.CodigoExterno == codigoExterno, cancellationToken);

    public Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken = default)
        => _context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);

    public async Task<IReadOnlyList<Usuario>> ListarAgentesAsync(CancellationToken cancellationToken = default)
        => await _context.Usuarios
            .Where(u => u.Papel == Domain.Enums.PapelUsuario.Agente
                || u.Papel == Domain.Enums.PapelUsuario.Supervisor
                || u.Papel == Domain.Enums.PapelUsuario.Admin)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => await _context.Usuarios.AddAsync(usuario, cancellationToken);
}