using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SupportAtos.Application.Abstractions;
using SupportAtos.Application.DTOs;
using SupportAtos.Application.Ports;
using SupportAtos.Domain.Entities;
using SupportAtos.Domain.Enums;

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

    public Task<ProvisioningResult> ProvisionAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
        => ProvisionInternalAsync(usuario, portalOnly: false, usarCache: true, cancellationToken);

    public Task<ProvisioningResult> ProvisionPortalAsync(PortalUserContext usuario, CancellationToken cancellationToken = default)
        => ProvisionInternalAsync(usuario, portalOnly: true, usarCache: false, cancellationToken);

    private async Task<ProvisioningResult> ProvisionInternalAsync(
        PortalUserContext usuario,
        bool portalOnly,
        bool usarCache,
        CancellationToken cancellationToken)
    {
        var chave = $"{CachePrefix}{usuario.CodigoExternoUsuario}:{usuario.ObterHashClaims()}";

        if (usarCache && _cache.TryGetValue(chave, out ProvisioningResult? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        for (var tentativa = 0; tentativa < MaxTentativasEmConflito; tentativa++)
        {
            try
            {
                using var escopo = _scopeFactory.CreateScope();
                var resultado = await ProvisionarEmEscopoAsync(
                    escopo.ServiceProvider, usuario, portalOnly, cancellationToken);

                if (usarCache)
                {
                    _cache.Set(chave, resultado, _cacheTtl);
                }

                return resultado;
            }
            catch (Exception exception) when (tentativa < MaxTentativasEmConflito - 1 && _uniqueViolationDetector.IsUniqueViolation(exception))
            {
                // Requisição concorrente persistiu o grupo/usuário: tentar contra o estado já salvo.
            }
        }

        throw new Exceptions.ConflictException("Não foi possível provisionar o usuário do portal (concorrência). Tente novamente.");
    }

    private async Task<ProvisioningResult> ProvisionarEmEscopoAsync(
        IServiceProvider servicos,
        PortalUserContext usuario,
        bool portalOnly,
        CancellationToken cancellationToken)
    {
        var grupoRepository = servicos.GetRequiredService<IGrupoEmpresaRepository>();
        var usuarioRepository = servicos.GetRequiredService<IUsuarioRepository>();
        var auditRepository = servicos.GetRequiredService<IProvisionamentoAuditRepository>();
        var unitOfWork = servicos.GetRequiredService<IUnitOfWork>();

        string acao;
        var mudou = false;
        var emailEmUso = await usuarioRepository.GetByEmailAsync(usuario.Email, cancellationToken);
        if (emailEmUso is not null
            && !string.Equals(emailEmUso.CodigoExterno, usuario.CodigoExternoUsuario, StringComparison.Ordinal))
        {
            throw new Exceptions.ConflictException("O e-mail informado já pertence a outro usuário.");
        }

        var idPortalGrupo = ObterIdPortal(usuario.CodigoExternoGrupo);
        var grupo = await grupoRepository.GetByCodigoExternoAsync(usuario.CodigoExternoGrupo, cancellationToken);
        if (grupo is null)
        {
            if (idPortalGrupo.HasValue)
            {
                var grupoComMesmoId = await grupoRepository.GetByIdAsync(idPortalGrupo.Value, cancellationToken);
                if (grupoComMesmoId is not null)
                {
                    throw new Exceptions.ConflictException(
                        "O ID do grupo informado já pertence a outro grupo de empresa.");
                }
            }

            grupo = GrupoEmpresa.Criar(usuario.NomeGrupo);
            grupo.AtribuirCodigoExterno(usuario.CodigoExternoGrupo);
            if (idPortalGrupo.HasValue)
            {
                grupo.DefinirIdPortal(idPortalGrupo.Value);
            }

            await grupoRepository.AddAsync(grupo, cancellationToken);
            mudou = true;
        }
        else if (grupo.Nome != usuario.NomeGrupo)
        {
            grupo.AlterarNome(usuario.NomeGrupo);
            mudou = true;
        }

        // Grupo recém-criado sem ID do portal só recebe o id no SaveChanges (identity do banco);
        // persistir antes para poder referenciá-lo ao criar o usuário.
        if (grupo.Id == 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var idPortalUsuario = ObterIdPortal(usuario.CodigoExternoUsuario);
        var usuarioAtual = await usuarioRepository.GetByCodigoExternoAsync(usuario.CodigoExternoUsuario, cancellationToken);
        if (usuarioAtual is null)
        {
            if (idPortalUsuario.HasValue)
            {
                var usuarioComMesmoId = await usuarioRepository.GetByIdAsync(idPortalUsuario.Value, cancellationToken);
                if (usuarioComMesmoId is not null)
                {
                    throw new Exceptions.ConflictException(
                        "O ID do usuário informado já pertence a outro usuário.");
                }
            }

            var papel = portalOnly ? PapelUsuario.Cliente : usuario.Papel;
            usuarioAtual = Usuario.Criar(usuario.Nome, usuario.Email, grupo.Id, papel);
            usuarioAtual.AtribuirCodigoExterno(usuario.CodigoExternoUsuario);
            if (idPortalUsuario.HasValue)
            {
                usuarioAtual.DefinirIdPortal(idPortalUsuario.Value);
            }

            await usuarioRepository.AddAsync(usuarioAtual, cancellationToken);
            acao = "Criado";
            mudou = true;
        }
        else
        {
            if (portalOnly && usuarioAtual.Papel != PapelUsuario.Cliente)
            {
                throw new Exceptions.ConflictException(
                    "O código externo informado pertence a um usuário que não é cliente do portal.");
            }

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

            // Upsert do portal nunca altera o papel: o papel é definido apenas
            // pela equipe/SSO. O portal só cria novos usuários como Cliente.
            if (!portalOnly && usuarioAtual.Papel != usuario.Papel)
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

    private static int? ObterIdPortal(string codigoExterno)
        => int.TryParse(codigoExterno, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id
            : null;
}