using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;

namespace SupportAtos.Application.Services;

public class UserProvisioningService : IUserProvisioningService
{
    private const string CachePrefix = "provisionamento:";
    private const int MaxTentativasEmConflito = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUniqueConstraintViolationDetector _uniqueViolationDetector;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl;

    public UserProvisioningService(
        IServiceScopeFactory scopeFactory,
        IUniqueConstraintViolationDetector uniqueViolationDetector,
        IMemoryCache cache,
        TimeSpan? cacheTtl = null)
    {
        _scopeFactory = scopeFactory;
        _uniqueViolationDetector = uniqueViolationDetector;
        _cache = cache;
        _cacheTtl = cacheTtl ?? TimeSpan.FromSeconds(60);
    }

    public async Task<ProvisioningResult> ProvisionAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
    {
        var chave = $"{CachePrefix}{usuario.CodigoExternoUsuario}:{usuario.ObterHashClaims()}";

        if (_cache.TryGetValue(chave, out ProvisioningResult? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        for (var tentativa = 0; tentativa < MaxTentativasEmConflito; tentativa++)
        {
            try
            {
                using var escopo = _scopeFactory.CreateScope();
                var resultado = await ProvisionarEmEscopoAsync(escopo.ServiceProvider, usuario, cancellationToken);

                _cache.Set(chave, resultado, _cacheTtl);
                return resultado;
            }
            catch (Exception exception) when (tentativa < MaxTentativasEmConflito - 1 && _uniqueViolationDetector.IsUniqueViolation(exception))
            {
                // Requisição concorrente persistiu o grupo/usuário: tentar contra o estado já salvo.
            }
        }

        throw new Exceptions.ConflictException("Não foi possível provisionar o usuário do portal (concorrência). Tente novamente.");
    }

    private async Task<ProvisioningResult> ProvisionarEmEscopoAsync(IServiceProvider servicos, PortalUserContext usuario, CancellationToken cancellationToken)
    {
        var grupoRepository = servicos.GetRequiredService<IGrupoEmpresaRepository>();
        var usuarioRepository = servicos.GetRequiredService<IUsuarioRepository>();
        var auditRepository = servicos.GetRequiredService<IProvisionamentoAuditRepository>();
        var unitOfWork = servicos.GetRequiredService<IUnitOfWork>();

        string acao;
        var mudou = false;
        var grupo = await grupoRepository.GetByCodigoExternoAsync(usuario.CodigoExternoGrupo, cancellationToken);
        if (grupo is null)
        {
            grupo = GrupoEmpresa.Criar(usuario.NomeGrupo);
            grupo.AtribuirCodigoExterno(usuario.CodigoExternoGrupo);
            await grupoRepository.AddAsync(grupo, cancellationToken);
            mudou = true;
        }

        // Grupo recém-criado só recebe o id no SaveChanges (identity do banco);
        // persistir antes para poder referenciá-lo ao criar o usuário.
        if (grupo.Id == 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var usuarioAtual = await usuarioRepository.GetByCodigoExternoAsync(usuario.CodigoExternoUsuario, cancellationToken);
        if (usuarioAtual is null)
        {
            usuarioAtual = Usuario.Criar(usuario.Nome, usuario.Email, grupo.Id, usuario.Papel);
            usuarioAtual.AtribuirCodigoExterno(usuario.CodigoExternoUsuario);
            await usuarioRepository.AddAsync(usuarioAtual, cancellationToken);
            acao = "Criado";
            mudou = true;
        }
        else
        {
            acao = "Atualizado";
            if (usuarioAtual.GrupoEmpresaId != grupo.Id)
            {
                usuarioAtual.AlterarGrupoEmpresa(grupo.Id);
                mudou = true;
            }

            if (usuarioAtual.Nome != usuario.Nome)
            {
                usuarioAtual.AlterarNome(usuario.Nome);
                mudou = true;
            }

            if (usuarioAtual.Email != usuario.Email)
            {
                usuarioAtual.AlterarEmail(usuario.Email);
                mudou = true;
            }

            if (usuarioAtual.Papel != usuario.Papel)
            {
                usuarioAtual.AlterarPapel(usuario.Papel);
                mudou = true;
            }
        }

        if (mudou)
        {
            await auditRepository.AddAsync(
                ProvisionamentoAudit.Registrar(usuario.CodigoExternoUsuario, usuario.CodigoExternoGrupo, acao), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new ProvisioningResult(usuarioAtual.Id, grupo.Id);
    }
}