# Documentação de Autenticação

## Objetivo

Este documento descreve como a API do SupportAtos autentica e autoriza:

- usuários da equipe interna, via JWT do SSO;
- usuários do portal, via API key de escopo `portal`;
- integrações máquina-a-máquina, via API key de escopo `automacao`.

Toda identidade autenticada é convertida em um `PortalUserContext`, armazenado em `HttpContext.Items` e consumido pelos casos de uso por meio de `ICurrentUser`.

## Visão geral

A API possui dois mecanismos independentes:

| Mecanismo | Header | Uso |
|---|---|---|
| JWT Bearer | `Authorization: Bearer <token>` | Equipe interna: Admin, Supervisor e Agente |
| API key | `X-Api-Key: <chave>` | Portal e automações |

As API keys são armazenadas apenas como hash SHA-256 na configuração. O valor em texto puro nunca deve ser versionado.

## Fluxo JWT (equipe)

1. O cliente envia o token no header `Authorization`.
2. O `JwtBearer` valida assinatura RS256, issuer, audience, expiração e clock skew.
3. `OnTokenValidated` lê as claims obrigatórias:
   - `sub`
   - `email`
   - `name`
   - `grupo_empresa_id`
   - `grupo_empresa_nome`
   - `role`
4. O `IUserProvisioningService.ProvisionAsync` faz upsert do grupo e do usuário.
5. O papel do token é convertido para `PapelUsuario` e regravado na identidade já com o nome canônico (`Agente`, `Cliente`, etc.).
6. O `PortalUserContext` provisionado é salvo em `HttpContext.Items`.
7. Falhas de infraestrutura/provisionamento retornam `500`, não `401`.

Configuração principal em `Sso`:

| Chave | Função |
|---|---|
| `Sso:Issuer` | Issuer aceito |
| `Sso:Audience` | Audience aceita |
| `Sso:JwksUrl` | JWKS do SSO em produção |
| `Sso:PublicKeyPem` | Chave pública PEM para desenvolvimento/testes |
| `Sso:ClockSkewSeconds` | Tolerância de relógio |
| `Sso:ClaimNames` | Nomes das claims esperadas |

`JwksUrl` e `PublicKeyPem` são alternativos. Pelo menos um deve existir.

## Fluxo API key do portal

Usado para consultar, responder e alterar chamados em nome de um usuário já provisionado.

1. O cliente envia `X-Api-Key` com uma chave de escopo `portal`.
2. O `ApiKeyAuthenticationHandler` calcula o SHA-256 e compara em tempo constante com `ApiKeys:Portal:PrimaryHash` ou `SecondaryHash`.
3. A identidade recebe a claim `scope=portal`.
4. A policy `Portal` ou `PortalOuEquipe` autoriza a requisição.
5. O `PortalUserContextMiddleware` lê os headers:
   - `X-Portal-User-Id`
   - `X-Portal-Group-Id`
6. O middleware busca usuário e grupo por `CodigoExterno`, valida que o usuário pertence ao grupo e salva o `PortalUserContext`.

Os headers de portal são obrigatórios para requisições de portal que não sejam a abertura de chamado. Se o usuário ou grupo não existir, ou não pertencerem ao mesmo grupo, a API retorna `404`.

### Exceção: abertura de chamado

`POST /api/chamados` não usa os headers `X-Portal-*`. O corpo da requisição contém `usuario` e `grupoEmpresa`, e o `AbrirChamadoPortalUseCase` chama `ProvisionPortalAsync`.

Nesse fluxo:

- usuário e grupo são criados ou atualizados a partir do corpo;
- usuário novo é sempre criado como `Cliente`;
- o papel de usuário existente nunca é alterado pelo portal;
- se o código externo pertencer a um usuário que não é cliente, a API retorna `409`;
- se o e-mail já pertencer a outro usuário, a API retorna `409`.

## Fluxo API key de automação

Usado por integrações como n8n.

- Header: `X-Api-Key`
- Escopo: `automacao`
- Endpoints: `/api/automacao/*` e `/api/webhooks/test`
- Não cria `PortalUserContext` de portal.
- Uma mesma chave não pode ser usada nos escopos `portal` e `automacao`.

A chave de saída de webhooks (`Webhooks:ApiKey`) deve ser diferente das chaves de entrada.

## Policies e endpoints

| Policy | Autenticação | Regra | Exemplos |
|---|---|---|---|
| `Portal` | API key | `scope=portal` | `POST /api/chamados` |
| `Automacao` | API key | `scope=automacao` | `/api/automacao/*`, `/api/webhooks/test` |
| `Jwt` | JWT | autenticado | `GET /api/usuarios/me` |
| `Equipe` | JWT | Admin, Supervisor ou Agente | `GET /api/usuarios`, atribuir chamado |
| `EquipeAdmin` | JWT | Admin | `POST /api/usuarios` |
| `PortalOuEquipe` | API key ou JWT | portal ou papel de equipe | consultar, responder e alterar status de chamados |

A `FallbackPolicy` exige usuário autenticado. Apenas `/health` é anônimo.

## Entidades

| Entidade | Campos principais | Papel na autenticação |
|---|---|---|
| `Usuario` | `Id`, `CodigoExterno`, `Nome`, `Email`, `GrupoEmpresaId`, `Papel` | Usuário provisionado do SSO ou do portal |
| `GrupoEmpresa` | `Id`, `CodigoExterno`, `Nome`, `Tipo`, `GrupoPaiId` | Grupo ao qual o usuário pertence |
| `ProvisionamentoAudit` | `CodigoExternoUsuario`, `CodigoExternoGrupo`, `Acao`, `Data` | Auditoria de criação/atualização no provisionamento |
| `LogAuditoria` | `ChamadoId`, `UsuarioId`, `Acao`, campos alterados | Auditoria de alterações em chamados |

Regras de persistência:

- `Usuario.Email` é único.
- `Usuario.CodigoExterno` é único.
- `GrupoEmpresa.CodigoExterno` é único.
- `Usuario` pertence a um `GrupoEmpresa`.
- `PapelUsuario`: `Cliente`, `Agente`, `Supervisor`, `Admin`.

## Provisionamento

O provisionamento é idempotente e usa cache em memória de 60 segundos para o fluxo JWT.

- Grupo é localizado por `CodigoExterno`; se não existir, é criado.
- Usuário é localizado por `CodigoExterno`; se não existir, é criado.
- Nome, e-mail, grupo e papel são atualizados quando necessário.
- O papel só é alterado no fluxo JWT; o fluxo de portal nunca promove um cliente.
- Conflitos de unicidade são tratados com até 3 tentativas.
- Criação ou atualização gera registro em `ProvisionamentoAudit`.

## Configuração e segredos

```json
{
  "Sso": {
    "Issuer": "...",
    "Audience": "...",
    "JwksUrl": "...",
    "PublicKeyPem": ""
  },
  "ApiKeys": {
    "Portal": {
      "PrimaryHash": "",
      "SecondaryHash": ""
    },
    "Automacao": {
      "PrimaryHash": "",
      "SecondaryHash": ""
    }
  }
}
```

- Segredos devem vir de user-secrets ou variáveis de ambiente.
- `appsettings.json` mantém apenas placeholders.
- `PrimaryHash` e `SecondaryHash` permitem rotação sem downtime.
- Fora de `Development`/`Testing`, a API key do portal é obrigatória.
- Hashes devem ter 64 caracteres hexadecimais (SHA-256).

Gerar uma chave:

```powershell
dotnet run --project src/backend/src/SupportAtos.Api -- --generate-api-key --escopo portal
```

O comando exibe a chave uma única vez e o hash que deve ser configurado.

> O README da raiz ainda menciona autenticação desativada e `Sso:ApiKey`. O comportamento atual é o descrito neste documento.

## Execução local e teste manual

### 1. Pré-requisitos

- .NET SDK 10;
- Docker Desktop ou PostgreSQL nativo;
- Node.js/npm para o frontend;
- `dotnet-ef` para migrations.

### 2. Banco local

Subir o PostgreSQL do Compose:

```powershell
docker compose -f src/backend/docker-compose.yml up -d
```

Connection string padrão do Compose:

```text
Host=localhost;Port=5433;Database=supportatos;Username=admin;Password=supportatos
```

Configurar o user-secret da API:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=supportatos;Username=admin;Password=supportatos" --project src/backend/src/SupportAtos.Api
```

> O `POSTGRES_PASSWORD` do Compose só é aplicado na primeira inicialização do volume. Se o volume já existir com outra senha, alterar o Compose não muda a senha. Nesse caso, redefina a senha do usuário ou recrie o volume.

### 3. Diagnóstico de senha do PostgreSQL

Verificar container, porta e variáveis:

```powershell
docker ps --filter "name=supportatos-db"
docker inspect supportatos-db --format "{{range .Config.Env}}{{println .}}{{end}}"
```

Em um ambiente já testado, o container estava ativo em `5433` e a variável indicava `POSTGRES_PASSWORD=admin`, mas o volume respondia `password authentication failed` para as senhas testadas. O log mostrava:

```text
FATAL: password authentication failed for user "admin"
DETAIL: Connection matched file "/var/lib/postgresql/data/pg_hba.conf" line 128: "host all all all scram-sha-256"
```

Como o acesso local por socket dentro do container usa `trust`, a senha foi redefinida:

```powershell
docker exec supportatos-db psql -U admin -d supportatos -c "ALTER USER admin WITH PASSWORD 'supportatos';"
```

Resultado:

```text
ALTER ROLE
```

Validação TCP externa:

```powershell
docker run --rm --add-host host.docker.internal:host-gateway -e PGPASSWORD=supportatos postgres:17 psql -h host.docker.internal -p 5433 -U admin -d supportatos -c "select current_user, current_database();"
```

Resultado:

```text
 current_user | current_database
--------------+------------------
 admin        | supportatos
(1 row)
```

Configuração no PgAdmin:

```text
Host: localhost
Port: 5433
Maintenance database: supportatos
Username: admin
Password: supportatos
SSL mode: Disable
```

Se o PgAdmin continuar falhando, remova o servidor e cadastre novamente para limpar a senha salva em cache.

### 4. Chave JWT de desenvolvimento

```powershell
dotnet run --project src/backend/tools/SupportAtos.TokenMinter -- generate

$pem = Get-Content -LiteralPath "src/backend/tools/SupportAtos.TokenMinter/keys/dev-public.pem" -Raw
dotnet user-secrets set "Sso:PublicKeyPem" $pem --project src/backend/src/SupportAtos.Api

dotnet run --project src/backend/src/SupportAtos.Api -- --check-dev-auth
```

O issuer e o audience padrão são `supportatos-dev` e `supportatos-api`. Não é necessário configurar `Sso:ApiKey`; essa chave pertence a uma versão antiga da configuração.

### 5. Migrations e seed

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/backend/src/SupportAtos.Infrastructure --startup-project src/backend/src/SupportAtos.Api

Get-Content .\src\backend\database\seed.sql -Raw | docker exec -i supportatos-db psql -U admin -d supportatos
```

O `seed.sql` cria as categorias de SLA necessárias para abrir chamados. O `seed_mock.sql` é legado do modo sem autenticação.

### 6. Subir a API

```powershell
dotnet run --project src/backend/src/SupportAtos.Api --launch-profile http
```

- API: `http://localhost:5177`
- Swagger: `http://localhost:5177/swagger`
- Health: `http://localhost:5177/health`

### 7. Testar JWT

```powershell
.\src\backend\scripts\mint-token.ps1 -Sub agente-teste -Email agente.teste@atos.com -Name "Agente Teste" -GrupoId grupo-teste -GrupoNome "Grupo Teste" -Papel Agente
```

O script copia o token para a área de transferência. No Swagger, clique em `Authorize` e cole apenas o token. Teste também via PowerShell:

```powershell
$token = Get-Clipboard
Invoke-RestMethod http://localhost:5177/api/usuarios/me -Headers @{ Authorization = "Bearer $token" }
```

Para validar permissões:

```powershell
# Funciona com Agente, Supervisor ou Admin
Invoke-RestMethod http://localhost:5177/api/usuarios -Headers @{ Authorization = "Bearer $token" }

# Token de Cliente deve retornar 403 nas rotas de equipe
.\src\backend\scripts\mint-token.ps1 -Sub cliente-teste -Email cliente.teste@atos.com -Name "Cliente Teste" -GrupoId grupo-teste -GrupoNome "Grupo Teste" -Papel Cliente
```

### 8. Testar API key do portal

Gerar as chaves:

```powershell
dotnet run --project src/backend/src/SupportAtos.Api -- --generate-api-key --escopo portal
dotnet run --project src/backend/src/SupportAtos.Api -- --generate-api-key --escopo automacao
```

Cada comando exibe a chave bruta uma única vez e o hash. Guarde a chave bruta e configure somente o hash:

```powershell
dotnet user-secrets set "ApiKeys:Portal:PrimaryHash" "<hash-portal>" --project src/backend/src/SupportAtos.Api
dotnet user-secrets set "ApiKeys:Automacao:PrimaryHash" "<hash-automacao>" --project src/backend/src/SupportAtos.Api
```

Reinicie a API. Exemplo de abertura de chamado:

```powershell
$portalKey = "<chave-bruta-portal>"

$body = @{
  usuario = @{ id = "cliente-teste"; nome = "Cliente Teste"; email = "cliente.teste@atos.com" }
  grupoEmpresa = @{ id = "grupo-teste"; nome = "Grupo Teste" }
  produto = "Portal"
  categoria = "Login"
  titulo = "Teste de autenticação"
  mensagemInicial = "Chamado aberto via API key do portal."
} | ConvertTo-Json -Depth 5

$resp = Invoke-RestMethod -Method Post -Uri http://localhost:5177/api/chamados `
  -Headers @{ "X-Api-Key" = $portalKey } `
  -ContentType "application/json" -Body $body

$resp.codigoPublico
```

Consultar o chamado com os headers de portal:

```powershell
Invoke-RestMethod -Uri "http://localhost:5177/api/chamados/$($resp.codigoPublico)" `
  -Headers @{
    "X-Api-Key" = $portalKey
    "X-Portal-User-Id" = "cliente-teste"
    "X-Portal-Group-Id" = "grupo-teste"
  }
```

### 9. Frontend local

```powershell
cd src/frontend
Copy-Item .env.example .env.local
npm install
npm run dev
```

No `.env.local`, manter:

```env
VITE_API_URL=http://localhost:5177
VITE_API_TOKEN=
```

O login atual do frontend ainda é mock e não grava o JWT. Para testar chamadas reais, use um token válido em `VITE_API_TOKEN` ou no navegador:

```javascript
sessionStorage.setItem('atos_token', '<jwt>')
```

### 10. Diagnóstico rápido

| Sintoma | Causa provável | Ação |
|---|---|---|
| `401` | Token ausente, expirado, assinado com outra chave ou API key não configurada | Rodar `--check-dev-auth`, conferir secrets e reiniciar a API |
| `403` | Papel ou escopo incorreto | Usar JWT de equipe ou API key do escopo correto |
| `404` no portal | Usuário/grupo não encontrado ou headers `X-Portal-*` incompatíveis | Conferir códigos externos e vínculo usuário/grupo |
| `409` | E-mail ou código externo já usado, ou papel incompatível | Ajustar dados do usuário/grupo |
| `429` | Rate limit excedido | Aguardar antes de repetir as requisições |
| PgAdmin com senha inválida | Senha salva em cache ou volume com senha antiga | Redefinir senha via `docker exec` e recadastrar o servidor |

## Ordem do pipeline

```text
ExceptionHandling
SecurityHeaders
SecurityRateLimit
StatusCodePages
CORS
HSTS/HTTPS (fora de Development/Testing)
Swagger (apenas Development)
Authentication
Authorization
PortalUserContextMiddleware
HealthChecks / Controllers
```

O `PortalUserContextMiddleware` roda depois de `Authorization` para que as claims da API key já estejam disponíveis em `HttpContext.User`.

O rate limiter permite 120 requisições por minuto por IP para requisições com API key e bloqueia temporariamente após 10 falhas de autenticação no mesmo minuto.

## Códigos de resposta

| Situação | Status |
|---|---|
| Token ou API key ausente/inválida | `401` |
| Autenticado, mas sem escopo ou papel necessário | `403` |
| Headers ou corpo inválidos | `400` |
| Usuário/grupo do portal não encontrado ou incompatível | `404` |
| Conflito de e-mail, código externo ou papel | `409` |
| Limite de requisições/falhas excedido | `429` |
| Falha de infraestrutura ou provisionamento | `500` |

## Testes

- `SessaoApiTests`: validação do JWT, claims e provisionamento.
- `SupportAtosApiTests`: fluxos de portal, API keys e policies da equipe.
- `ApiKeySecurityTests`: hash, comparação e validação de configuração.

Comandos:

```powershell
dotnet build src/backend/SupportAtos.slnx --nologo
dotnet test src/backend/SupportAtos.slnx --nologo
.\src\backend\tests\SupportAtos.Qa.Tests\run-qa.ps1 -Group ALL
```

Resultado da última execução local:

| Suíte | Resultado |
|---|---|
| Build | 0 erros, 0 warnings |
| Application | 34 testes aprovados |
| Integration | 47 testes aprovados |
| QA | 18 testes aprovados |
| Total | 99 testes aprovados |

Os testes de integração usam o PostgreSQL do Docker na porta `5433`, criam automaticamente o banco `supportatos_test` e aplicam as migrations. A conexão pode ser sobrescrita com `SUPPORTATOS_TEST_CONNECTION`.

Os testes de integração usam um RSA compartilhado para assinar JWTs. O acesso a essa chave é serializado em `TestTokenFactory` para evitar falhas intermitentes quando as classes de teste executam em paralelo.

## Arquivos de referência

- `src/backend/src/SupportAtos.Api/Authentication/AuthenticationSetup.cs`
- `src/backend/src/SupportAtos.Api/Authentication/ApiKeyAuthenticationHandler.cs`
- `src/backend/src/SupportAtos.Api/Authentication/PortalUserResolver.cs`
- `src/backend/src/SupportAtos.Api/Middleware/PortalUserContextMiddleware.cs`
- `src/backend/src/SupportAtos.Application/Services/UserProvisioningService.cs`
- `src/backend/src/SupportAtos.Api/Program.cs`
- `src/backend/tests/SupportAtos.Integration.Tests/`
