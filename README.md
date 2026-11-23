# SupportAtos

[![CI](https://github.com/FluixIT/system-atos-squad74/actions/workflows/ci.yml/badge.svg)](https://github.com/FluixIT/system-atos-squad74/actions/workflows/ci.yml)
[![E2E Playwright](https://github.com/FluixIT/system-atos-squad74/actions/workflows/e2e.yml/badge.svg)](https://github.com/FluixIT/system-atos-squad74/actions/workflows/e2e.yml)

Sistema de suporte com portal do cliente e painel de atendimento. Backend em C#/.NET 10, ASP.NET Core, EF Core 10 e PostgreSQL; frontend em React 18, TypeScript, Vite e Material UI.

## Setup Windows/PowerShell — Etapa B

O setup local agora é preparado por `setup.ps1`. Execute em Windows PowerShell 5.1 ou PowerShell 7, em terminal interativo. Os scripts resolvem caminhos a partir do checkout e não exigem execução como administrador; o Windows pode pedir confirmação para confiar no certificado HTTPS.

```powershell
# Somente leitura: verifica ferramentas/Docker; nao instala nem altera recursos.
.\setup.ps1 -CheckOnly

# Preparacao interativa: pode instalar dependencias e criar configuracao local.
.\setup.ps1

# Rotina diaria: API em background, Vite neste terminal.
.\start.ps1
```

Se a política do PowerShell impedir execução, siga a política aprovada pela equipe para scripts locais; os scripts não alteram ExecutionPolicy. Não usar pipes, redirecionamento ou gravação de sessões ao preparar credenciais. Senhas são digitadas sem eco, transmitidas por stdin/ambiente de processos filhos e nunca adicionadas aos argumentos ou arquivos do repositório.

### O que o setup faz

1. Confere Git, Docker Linux local/Compose v2, SDK .NET 10 ou superior (incluindo Preview), runtimes .NET/ASP.NET Core 10, Node 22+ e npm. Preview gera aviso informativo, sem bloqueio. Não instala esses pré-requisitos automaticamente.
2. Restaura `dotnet-ef` **10.0.0**, pelo manifesto local `.config/dotnet-tools.json`, sem depender da instalação global; compila API/utilitário em Release.
3. Diagnostica User Secrets, provedores da API e aliases de conexão no ambiente Process/User/Machine. O destino exclusivo é `127.0.0.1:<SUPPORTATOS_DB_PORT>/supportatos` (padrão `5433`), usuário `admin`, sem schema/Options alternativos; `localhost` na mesma porta existente é reconhecido. Para substituir conexão antiga, exige `CONFIGURAR DOCKER supportatos`, solicita a senha Docker sem eco quando necessária, verifica credenciais/cluster e cria backup protegido antes de gravar somente `ConnectionStrings:DefaultConnection` nos User Secrets. Não muda a senha no PostgreSQL. Uma conexão antiga não é mais um bloqueio definitivo: pode ser substituída com consentimento; cancelar mantém a configuração original.
4. Usa somente o Compose oficial e verifica o volume `backend_supportatos-db-data`. Container em execução é reutilizado; parado exige `INICIAR supportatos-db`. **Nunca recria containers existentes**; usa `--no-recreate`. Alterações de binding da Etapa A continuam pendentes até uma atualização manual autorizada. Não executa Compose legado, SQL histórico, `down` ou remoções de recursos Docker.
5. Valida conexão, PostgreSQL 17 e identidade do cluster comparando `system_identifier` pelo host e pelo container. Essa leitura exige o usuário administrativo `admin` do Compose. Credencial errada não provoca reset/recriação: informe a senha correta nos User Secrets fora do Git e repita.
6. Preserva JWT efetivo válido ou reutiliza o par em `tools/SupportAtos.TokenMinter/keys/` (ignorado pelo Git). Gera par somente quando ambos os arquivos e a configuração privada estão ausentes. Pares incompletos/divergentes ou configuração local conflitante interrompem o processo; não há rotação automática. Configura somente valores locais ausentes de Auth/Sso.
7. Executa `npm ci`, resolve os overrides reais do Vite, prepara/confia no certificado ASP.NET Core e exporta PEM/chave somente quando o par padrão está ausente. Arquivos existentes são validados quanto a correspondência, validade, localhost e confiança; não são sobrescritos. Certificados personalizados ausentes ou pares parciais exigem correção manual. Não usa `dev-certs --clean`.
8. Aplica migrations pendentes **somente** após digitar `MIGRAR 127.0.0.1:<SUPPORTATOS_DB_PORT>/supportatos` (padrão `5433`). Usa o EF local com a conexão exata validada no ambiente do processo filho, sem senha nos argumentos; não lê outra conexão após a confirmação. Enter cancela a etapa. Em banco existente, providencie backup antes de confirmar.
9. Se houver Admin ou marcador, preserva-os e não executa bootstrap. Caso contrário oferece `ADMIN` para abrir o comando interativo seguro da API; Enter deixa a criação pendente. O comando mantém sua confirmação própria `INICIALIZAR supportatos` e senha sem eco. Admin preexistente sem credencial exige recuperação por procedimento próprio; não será recriado pelo setup.

O utilitário auxiliar `SupportAtos.DevSetup` usa os provedores da API em Development e omite diagnósticos de subprocessos que possam conter segredos. A conexão verificada é aplicada ao ambiente do processo pai e herdada por API, EF e bootstrap, com remoção temporária dos aliases conflitantes; ao terminar, o ambiente anterior é restaurado. **Variáveis permanentes de usuário/máquina não são modificadas.** User Secrets continuam fora do Git, mas não são cofre criptografado. A senha inicial do PostgreSQL também fica no ambiente interno do container, como no Compose original; não publique `docker inspect` completo.

Reexecutar preserva recursos e credenciais, não repete migrations já aplicadas nem recria Admin. `npm ci` reinstala dependências a partir do lockfile. Se houver cancelamento ou falha, etapas anteriores concluídas permanecem; não há rollback destrutivo. Corrija o item indicado e execute novamente. Não execute dois setups simultaneamente.

### Troca de configuração antiga e backups

O desenvolvimento do SupportAtos usa exclusivamente PostgreSQL 17 no Docker. Instalações PostgreSQL Windows são preservadas, mas nunca são oferecidas ou acessadas como banco deste projeto. API, factory EF e bootstrap em **Development** recusam conexões fora do destino oficial antes de abrir conexão. Production/Testing não recebem essa restrição de desenvolvimento.

`setup.ps1 -CheckOnly` e `start.ps1 -CheckOnly` leem configurações e metadados Docker, sem abrir conexão com bancos, compilar, instalar, criar backups ou modificar arquivos/variáveis. Mostram somente origem e classificação, sem connection strings/senhas. Código de saída: `0` sem pendências detectadas, `2` para conexão ausente/divergente e `1` para falha de pré-requisito/diagnóstico. Em clone limpo, o placeholder é uma pendência esperada. São inspecionados também overrides em launch profiles; `.env` não é fonte automática da API/EF. JSON inválido é reportado sem imprimir conteúdo; no diagnóstico PowerShell, use JSON padrão sem comentários.

Backups ficam em `%LOCALAPPDATA%\SupportAtos\setup-private\backups\<id>.dpapi`, fora do checkout: criptografia Windows DPAPI **CurrentUser**, ACL sem herança permitindo somente o usuário atual e criação sem sobrescrita. Contêm os bytes originais dos User Secrets (incluindo os demais segredos), valores antigos de ambiente por escopo e fontes da conexão. A recuperação exige o mesmo usuário/contexto Windows; não são backups portáveis nem substituem backup dos dados PostgreSQL. Não copiar, publicar ou versionar esses arquivos. A dependência `System.Security.Cryptography.ProtectedData` está restrita ao utilitário de setup.

A conexão passa do utilitário .NET ao PowerShell por arquivo DPAPI de uso único em `setup-private\sessions`, removido após leitura. Não trafega por stdout, argumentos ou arquivos do repositório. JWT, certificados e demais User Secrets não são substituídos pela troca de conexão.

Para validarmos a troca real **posteriormente, com autorização**, sem migrations/bootstrap:

```powershell
.\setup.ps1 -CheckOnly
# Quando autorizada a substituicao da conexao:
.\setup.ps1 -ConnectionOnly
# Confirmar CONFIGURAR DOCKER supportatos; informar a senha real do container.
.\setup.ps1 -CheckOnly
```

`-ConnectionOnly` restaura/compila ferramentas, verifica o Docker oficial e configura somente a conexão; não executa JWT, certificados, npm, migrations ou bootstrap. Container em execução e volume existente são reutilizados, nunca recriados. Se parado, exige confirmação para iniciar; em clone limpo pode criar o container oficial sem apagar volumes. Senha incorreta, identidade divergente, falha de backup ou cancelamento impedem a substituição dos User Secrets. Nenhum acesso ao PostgreSQL Windows é feito para testar a senha antiga.

Se houver variável permanente antiga, ela continuará visível no diagnóstico mesmo após salvar User Secrets Docker. `start.ps1` pedirá `USAR DOCKER NESTA SESSAO`, fará backup protegido e aplicará apenas overrides temporários aos filhos. A remoção permanente dessas variáveis é uma decisão manual separada. Scripts restauram o ambiente ao sair; um novo terminal pode herdar novamente os valores antigos. Prefira os wrappers; comandos manuais em Development falharão se herdarem uma conexão Windows.

### Inicialização diária

`start.ps1` verifica os pré-requisitos, configuração JWT, identidade/schema do banco e certificado Vite; não inicia/recria Docker, não instala pacotes e não aplica migrations. Execute o setup primeiro e mantenha o Docker preparado. URLs permanecem `https://localhost:7099` e `https://localhost:5173`. O script recusa portas ocupadas e nunca encerra instâncias preexistentes.

No modo padrão, a API inicia sem janela adicional; Vite permanece no terminal. Ao finalizar, o script encerra somente a API criada por essa execução. Para diagnóstico ou terminais separados:

```powershell
.\start.ps1 -CheckOnly       # pre-requisitos/conexoes/metadados Docker; sem acesso a banco
.\start.ps1 -Service Api    # API em primeiro plano, com diagnosticos
# Em outro terminal:
.\start.ps1 -Service Frontend
```

Ctrl+C encerra a sessão; fechamento forçado do terminal pode exigir encerrar manualmente a instância criada. Após alterações de código backend, compile novamente ou reexecute setup; `start.ps1` usa o build Release preparado e não é hot reload.

A autenticação está ativa: funcionários usam e-mail/senha, JWT RS256 e refresh token em cookie HttpOnly/Secure/SameSite=Lax. Não há sessão mock no fluxo recomendado. Integrações servidor a servidor usam API keys próprias.

## Arquitetura e requisitos

- Domain: entidades e regras de domínio.
- Application: casos de uso, DTOs e contratos.
- Infrastructure: repositórios, EF Core, migrations e PostgreSQL.
- Api: controllers, autenticação, middlewares e composição.
- Frontend: SPA React que chama a API diretamente, sem proxy Vite.

Requer SDK .NET 10 ou superior, incluindo versões Preview (solução `.slnx`, alvo `net10.0`). **.NET 10 estável é recomendado, não obrigatório.** Os runtimes .NET/ASP.NET Core 10 continuam necessários para executar o projeto e o EF fixado; instalar apenas SDK/runtime de uma versão superior não garante essa execução. Requer também Node.js 22 LTS/npm recomendado (mínimo aceito 22), Docker Desktop com Engine Linux local e Compose v2 com `--wait-timeout`/`--no-recreate`. EF CLI 10.0.0 permanece fixado no manifesto local; o target `net10.0` não muda. A seleção exata do SDK/Node ainda não tem pinagem. PostgreSQL nativo e `psql` no Windows não são necessários no fluxo Docker.

A validação usa `dotnet --version` dentro do checkout, respeitando PATH e eventual `global.json`, em vez de aceitar qualquer SDK apenas listado como instalado. Avalia o projeto real com MSBuild sem restore/build, confere os runtimes 10 e executa `--version` do EF local quando restaurado, comparando com o manifesto. Se a ferramenta ainda não estiver disponível, `-CheckOnly` informa a pendência sem instalá-la. O setup completo restaura/executa o EF fixado e compila a API/utilitário antes de configurar recursos; falhas reais continuam bloqueando a preparação, mesmo em SDKs aceitos pela versão.

## Endereços oficiais

| Serviço | Endereço |
| --- | --- |
| PostgreSQL no host | `127.0.0.1:<SUPPORTATOS_DB_PORT>`, banco `supportatos`, usuário `admin` (padrão `5433`) |
| API | `https://localhost:7099` |
| Swagger / saúde do host | `https://localhost:7099/swagger` / `https://localhost:7099/health` |
| Frontend | `https://localhost:5173` |

`VITE_API_URL` usa `https://localhost:7099` por padrão. Manter `localhost` nos URLs HTTPS: trocar por `127.0.0.1` altera origem, certificado e contexto de cookies. Os endereços HTTP anteriores da API permanecem para compatibilidade; o fluxo recomendado usa explicitamente `--launch-profile https`.

## PostgreSQL e dados persistentes

O único Compose oficial é [src/backend/docker-compose.yml](src/backend/docker-compose.yml): PostgreSQL 17, publicação `127.0.0.1:${SUPPORTATOS_DB_PORT:-5433}:5432` (porta HOST configurável, porta interna fixa `5432`), healthcheck e volume persistente em `/var/lib/postgresql/data`. Sobe somente o banco; API e frontend rodam no host.

Serviço/container `supportatos-db` e chave de volume `supportatos-db-data` foram preservados. Com o projeto padrão `backend`, o volume existente é **`backend_supportatos-db-data`**. Não mudar `-p`, `COMPOSE_PROJECT_NAME`, diretório do Compose ou usar outro Compose sem conferir a identidade do volume. Outro projeto pode selecionar outro volume e aparentar perda dos dados.

[Db.SuporteAtos/docker-compose.yml](src/backend/Db.SuporteAtos/docker-compose.yml) é legado e não deve ser executado no onboarding. Disputa container/porta, seleciona outro volume e inicializa SQL histórico incompatível com o fluxo EF. Está preservado para referência.

PostgreSQL Windows, inclusive serviços em `5432`, permanece intacto e fora do fluxo de desenvolvimento do SupportAtos. Não parar serviços, desinstalar, testar credenciais ou migrar dados dessa instalação. O projeto usa exclusivamente o Docker na porta configurada em `SUPPORTATOS_DB_PORT` (padrão `5433`); se essa porta estiver ocupada na sua máquina, escolha outra.

### Porta local do PostgreSQL

Descoberta de porta no onboarding: configuracoes explicitas no arquivo local e no ambiente prevalecem (divergencia gera erro). Sem elas, o binding unico em loopback do container oficial, inclusive parado, determina a porta. Sem container, procura uma porta livre de 5433 a 5443. Identidade divergente interrompe a descoberta. O setup salva a porta somente apos validar a conexao, sem sobrescrever arquivo existente. CheckOnly informa a origem sem persistir ou exportar a porta. A disponibilidade nao reserva a porta ate a subida do Docker. Arquivo local existente sem porta v lida exige corre  o manual e permanece preservado.

Cada máquina pode publicar o Docker em uma porta diferente sem alterar arquivos versionados. Copie `.supportatos.local.env.example` para `.supportatos.local.env` na raiz do repositório (ignorado pelo Git) e ajuste:

```text
SUPPORTATOS_DB_PORT=5434
```

`setup.ps1`, `start.ps1` e o utilitário `SupportAtos.DevSetup` leem esse arquivo e exportam a mesma variável para o Compose, a API, o EF Core e o bootstrap. A conexão em User Secrets deve usar exatamente a mesma porta; o setup grava a porta configurada e o diagnóstico recusa uma connection string divergente antes de abrir conexão.

O Compose **não** carrega automaticamente esse arquivo por estar em outro diretório. Para comandos manuais, informe-o explicitamente a partir de `src/backend`:

```powershell
docker compose --env-file ../../.supportatos.local.env -f docker-compose.yml config
```

Se a porta escolhida estiver ocupada, o setup exibe a mensagem e não encerra processos de outros projetos; escolha outra porta. Alterar a porta de um container já existente exige recriação autorizada, preservando o volume (não use `down -v`):

```powershell
docker compose --env-file ../../.supportatos.local.env -f docker-compose.yml up -d --force-recreate --no-deps supportatos-db
```

A recriação acima modifica o container real; execute-a somente quando autorizado e após conferir que o volume `backend_supportatos-db-data` será reutilizado. Ela não apaga dados nem altera a senha já armazenada.

O Compose mantém o fallback de senha local anterior por compatibilidade. Em volume novo, definir `POSTGRES_PASSWORD` localmente antes da primeira inicialização. Em volume existente, preservar a senha real: alterar a variável **não altera a senha armazenada**. Não versionar credenciais, apagar volumes ou usar `down -v` como solução de onboarding. A publicação em loopback e a porta configurada só passam a valer na próxima recriação autorizada do container; esta etapa não o recria.

## Conexão da aplicação e do EF Core

Aplicação e factory de design-time usam os provedores padrão da API. Precedência de `ConnectionStrings:DefaultConnection`, da maior para a menor:

1. Argumentos de configuração (no EF, após `--`). Evitar senhas em argumentos/histórico.
2. Ambiente: `ConnectionStrings__DefaultConnection`.
3. User Secrets de `SupportAtos.Api`, **somente em Development**.
4. `appsettings.{Environment}.json`.
5. `appsettings.json`.

Usar o mesmo ambiente e overrides em ambos os processos. Os wrappers fixam a conexão verificada no ambiente dos filhos, acima dos User Secrets; aliases antigos são removidos apenas da sessão e restaurados ao sair. A factory encontra a pasta da API a partir do checkout, inclusive da raiz ou de `src/backend`, sem iniciar host, seeders ou autenticação e sem abrir conexão/aplicar migrations por conta própria. Em Development, a política compartilhada com API/setup valida o destino Docker antes do contexto ser criado.

**EF não lê launchSettings.json.** Passar `-- --environment Development` para carregar User Secrets. Sem ambiente explícito/variáveis, o padrão é Production. `DOTNET_ENVIRONMENT` tem prioridade sobre `ASPNETCORE_ENVIRONMENT`; `--environment` seleciona explicitamente o ambiente. O appsettings Development contém somente um placeholder na porta padrão; fora dos wrappers, defina `SUPPORTATOS_DB_PORT` no processo e configure a senha local pelo setup. Em Development, API/EF também recusam uma porta diferente da configurada.

Exemplo de destino Docker, sem credencial pronta:

```text
Host=127.0.0.1;Port=<SUPPORTATOS_DB_PORT>;Database=supportatos;Username=admin;Password=<senha local existente>
```

Guardar a conexão em User Secrets da API ou ambiente do processo. User Secrets ficam fora do Git, mas não são cofre criptografado. Arquivos `.env` do Compose/Vite não são carregados automaticamente pela API/EF; a porta local usa `.supportatos.local.env` lido pelos wrappers e o Compose manual usa `--env-file`. No frontend, usar `.env.local`, ignorado pelo Git. Variáveis `VITE_*` vão para o navegador e não devem conter segredos.

## Schema, dados de desenvolvimento e Admin

Migrations em `src/backend/src/SupportAtos.Infrastructure/Migrations` são a fonte oficial do schema. O PostgreSQL cria o banco configurado no primeiro uso de volume vazio. A API não aplica migrations automaticamente; o seeder Development existente cria apenas grupos quando a tabela está vazia e exige schema previamente atualizado.

Não executar SQLs históricos de `database/`, `src/backend/database/` ou do Compose legado no fluxo recomendado. `seed.sql` usa a tabela antiga `sla_categoria`; `seed_mock.sql` cria Admin sem credencial e impede o bootstrap. Arquivos preservados. Um seed funcional idempotente de SLA/grupos ainda será preparado; iniciar a API e fazer login não garante abertura de chamados sem esses dados e vínculos empresariais.

Em banco existente, conferir destino, histórico e backup antes de migrations. Para futura preparação autorizada, a partir da raiz:

```powershell
# ALTERA o schema; não é uma verificação estática.
dotnet ef database update --project src/backend/src/SupportAtos.Infrastructure --startup-project src/backend/src/SupportAtos.Api -- --environment Development
```

Após schema atualizado, criar o primeiro Admin pelo bootstrap interativo existente:

```powershell
# Cria dados; somente na preparação autorizada de banco sem Admin/bootstrap.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --no-launch-profile --project src/backend/src/SupportAtos.Api -- --bootstrap-admin
```

O comando pede senha sem eco e confirmação do banco; usa transação, bloqueio PostgreSQL e marcador persistente. Não aplica migrations, não possui senha padrão ou rota HTTP. Não apagar marcador/Admin para repetir. Ver [autenticação — fase 3](docs/autenticacao-fase3.md).

## Autenticação e HTTPS: configuração complementar

Configurar localmente `Auth:Issuer`, `Auth:Audience`, `Auth:PrivateKeyPem` e a chave correspondente `Sso:PublicKeyPem`. Em Development, alinhar emissor/público com `Sso:Issuer=supportatos-dev` e `Sso:Audience=supportatos-api`, salvo overrides locais intencionais. Preservar pares JWT existentes; não gerar/sobrescrever chaves na rotina.

Preparar certificado confiável e exportar par PEM somente se ausente, conforme [README do frontend](src/frontend/README.md#como-rodar). O Vite lê `~/.aspnet/https/supportatos-frontend.pem` e `.key`; aceita `SUPPORTATOS_HTTPS_CERT` e `SUPPORTATOS_HTTPS_KEY`. Não guardar chave no Git ou sobrescrever par existente. Se só um arquivo existir, restaurar o par antes de iniciar.

CORS permite `https://localhost:5173` com credenciais. CSRF exige Origin/Referer permitido em login, refresh, logout e ativação; chamadas manuais sem esses cabeçalhos recebem 403. Preservar proteções e cookies Secure. TLS/cookies reais exigem validação em navegador; `vite preview` não substitui o ambiente autenticado.

## Alternativa de inicialização manual

Na raiz, com Docker disponível, identidade do volume conferida e schema/segredos/certificados preparados:

```powershell
docker compose -f src/backend/docker-compose.yml up -d --wait
dotnet restore src/backend/SupportAtos.slnx
dotnet run --project src/backend/src/SupportAtos.Api --launch-profile https
```

`compose up` pode recriar o container para aplicar configuração; planejar essa atualização em ambiente existente. O volume deve ser preservado. Em outro terminal:

```powershell
cd src/frontend
npm ci
npm run dev
```

Abrir `https://localhost:5173` e usar conta ativada. `/health` verifica o host, não PostgreSQL ou migrations. Em conflito de portas, identificar a instância específica; não encerrar indiscriminadamente todos os processos dotnet.

## Verificações sem banco

```powershell
docker compose -f src/backend/docker-compose.yml config --quiet
dotnet build src/backend/SupportAtos.slnx
dotnet test src/backend/tests/SupportAtos.Application.Tests
dotnet test src/backend/tests/SupportAtos.Integration.Tests --filter FullyQualifiedName~DesignTimeConfigurationTests
cd src/frontend
npm run typecheck
```

Não executar a suíte completa de integração como teste estático: ela cria/aplica migrations e grava dados. Usar banco descartável explícito; a [suíte QA](src/backend/tests/SupportAtos.Qa.Tests/README.md) tem regras próprias de isolamento.

Validações sem banco: `powershell -NoProfile -File scripts/Development.Sdk.Tests.ps1`, `powershell -NoProfile -File scripts/Development.Connection.Tests.ps1`, `node --test scripts/frontend-config.test.mjs` e `dotnet test src/backend/tests/SupportAtos.Integration.Tests -c Release --filter 'FullyQualifiedName~DevelopmentSetupSafetyTests|FullyQualifiedName~DesignTimeConfigurationTests|FullyQualifiedName~DockerConnectionSetupTests'`. Os testes usam configurações sintéticas, metadados Docker simulados e fixtures temporários; DPAPI/ACL são testados somente em Windows. `-CheckOnly` não exercita substituição real de segredos, certificados, migrations, bootstrap ou inicialização real.

Ainda exige teste em clone limpo Windows com SDK 10+ (preferencialmente 10 estável): primeira preparação do EF, criação do volume/container, persistência após nova execução, senha com caracteres especiais no PostgreSQL real, confiança/exportação de certificados, confirmação/cancelamento de migrations, bootstrap interativo e ciclo start/Ctrl+C. Não executar essas etapas em ambiente existente sem autorização. Seed funcional de SLA/grupos e vínculos empresariais permanece pendente; nenhuma SQL legada é executada. Arquitetura e requisitos: [docs/](docs/README.md).
