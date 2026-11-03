using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Exceptions;
using SupportAtos.Application.Mappings;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.UseCases;

public class CadastrarUsuarioUseCase
{
    private readonly IGrupoEmpresaRepository _grupoEmpresaRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarUsuarioUseCase(
        IGrupoEmpresaRepository grupoEmpresaRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _grupoEmpresaRepository = grupoEmpresaRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UsuarioDto> Handle(CadastrarUsuarioInput input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Nome))
        {
            throw new ValidationException("O nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(input.Email))
        {
            throw new ValidationException("O e-mail é obrigatório.");
        }

        if (input.GrupoEmpresaId <= 0)
        {
            throw new ValidationException("O grupo de empresa é obrigatório.");
        }

        var grupo = await _grupoEmpresaRepository.GetByIdAsync(input.GrupoEmpresaId, cancellationToken)
            ?? throw new ValidationException($"O grupo de empresa '{input.GrupoEmpresaId}' não existe.");

        var email = input.Email.Trim().ToLowerInvariant();
        if (await _usuarioRepository.EmailExisteAsync(email, cancellationToken))
        {
            throw new ConflictException("Já existe um usuário cadastrado com esse e-mail.");
        }

        var usuario = Usuario.Criar(input.Nome, email, grupo.Id, input.Papel);

        await _usuarioRepository.AddAsync(usuario, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapper.ToDto(usuario, grupo.Nome);
    }
}