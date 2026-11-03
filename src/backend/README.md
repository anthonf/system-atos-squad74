# SupportAtos API

Backend do sistema de suporte (portal do cliente + painel do atendente). Se você é do squad e só quer **rodar e testar**, segue o roteiro abaixo — dá uns 10 minutos na primeira vez e 2 minutos no dia a dia.

Stack: **C# / .NET 10**, **Clean Architecture**, **EF Core 10** + **PostgreSQL**.

> Todos os comandos desta página assumem que seu terminal está em `src/backend/`.

## O que você precisa

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL rodando (nativo na `5432` **ou** Docker na `5433` — escolha um, ver passo 1)
- Node 20+ (só se for rodar o frontend junto)
- Tool do EF Core (uma vez só):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Primeira vez (do zero)

### 1. Suba o banco

**Opção A — Postgres nativo (recomendado no dia a dia):**
```bash
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE supportatos;"
```

**Opção B — Docker (bom para testes):**
```bash
docker compose up -d   # sobe na porta 5433, usuário admin / senha supportatos
```

### 2. Guarde os segredos na sua máquina (não vão para o git)

O `appsettings.Development.json` versionado tem só placeholders (`__SUA_SENHA__`). Os valores reais ficam no **User Secrets**, fora do repositório:

```bash
cd src/SupportAtos.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=supportatos;Username=postgres;Password=SUA_SENHA"
dotnet user-secrets set "Sso:ApiKey" "QUALQUER_VALOR_LOCAL"
```

Agora a chave pública que valida os tokens de dev (é pública, pode versionar — mas o jeito mais seguro é via secrets também):

```powershell
# no PowerShell, ainda em src/backend/
$pem = Get-Content -LiteralPath "tools\SupportAtos.TokenMinter\keys\dev-public.pem" -Raw
dotnet user-secrets set "Sso:PublicKeyPem" $pem --project src/SupportAtos.Api
```

> Se a pasta `keys/` estiver vazia, gere o par antes: `dotnet run --project tools/SupportAtos.TokenMinter -- generate`

### 3. Crie as tabelas

```bash
dotnet ef database update --project src/SupportAtos.Infrastructure --startup-project src/SupportAtos.Api
```

### 4. Compile

```bash
dotnet build SupportAtos.slnx
```

### 5. Rode a API

Aqui tem um detalhe: em máquina com OneDrive + política de aplicativos, o `dotnet run` pode ser bloqueado. O jeito que sempre funciona é rodar a DLL direto:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Obrigatório rodando pela DLL: ela ignora o launchSettings e sem isso sobe na porta 5000 (o frontend espera a 5177)
$env:ASPNETCORE_URLS = "http://localhost:5177"
Set-Location -LiteralPath "src\SupportAtos.Api\bin\Debug\net10.0"
dotnet SupportAtos.Api.dll
```

Abra `http://localhost:5177/swagger`. Se abriu, o backend está no ar. 🎉

### 6. Gere seu token e teste

Em **outra janela** do PowerShell (o backend fica preso na primeira):

```powershell
Set-Location -LiteralPath "<caminho>\src\backend"
.\scripts\mint-token.ps1 -Sub portal-admin -Email admin@atos.com -Name "Admin" -GrupoId grupo-1 -GrupoNome "Grupo Norte" -Admin
```

O script copia o JWT para a área de transferência. No Swagger, clique em **Authorize**, cole `Bearer <token>` e chame `GET /api/usuarios/1` — esperado: **200**.

## Todo dia (rotina de 2 minutos)

O segredo já está salvo, o banco já existe. Só isso:

```powershell
# Janela 1 — backend (mate instâncias velhas antes: elas seguram a porta e respondem com chave/token antigos)
Get-Process -Name dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Set-Location -LiteralPath "<caminho>\src\backend"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5177"
Set-Location -LiteralPath "src\SupportAtos.Api\bin\Debug\net10.0"
dotnet SupportAtos.Api.dll

# Janela 2 — token novo (só se o de ontem expirou; padrão 60 min) + frontend
Set-Location -LiteralPath "<caminho>\src\backend"
.\scripts\mint-token.ps1 -Sub portal-agente -Email agente@atos.com -Name "Agente" -GrupoId grupo-1 -GrupoNome "Grupo Norte"
Set-Location -LiteralPath "<caminho>\src\frontend"
npm run dev   # http://localhost:5173
```

### Se algo der errado (2 min)

- Fila/Dashboard com "Erro interno do servidor" → o backend não está na 5177. Confira `http://localhost:5177/health` (tem que dar `Healthy`); o proxy do frontend só fala com a 5177.
- `401` no Swagger/front → token expirou (padrão 60 min, gere outro no passo 6) ou tem instância antiga rodando (mate os `dotnet` e suba de novo).
- Backend nem sobe ("address already in use") → processo antigo segurando a porta; rode o `Stop-Process` da Janela 1 e tente de novo.

## O banco em 30 segundos

- **PostgreSQL**, acesso via EF Core. O SQL em `database/` é **legado** — a fonte oficial do schema são as **Migrations** (`src/SupportAtos.Infrastructure/Migrations`).
- Tabelas principais: `grupo_empresa`, `usuario`, `chamado`, `interacao`, `anexo`, `sla_config`, `log_auditoria` (tudo em `snake_case`, enums como texto).
- No primeiro login com JWT, a API **cria sozinha** o usuário e o grupo a partir das claims do token (provisionamento JIT). Não precisa cadastrar ninguém na mão.
- Em `Development`, um seeder garante grupos de teste se a tabela estiver vazia.


## Endpoints (dev)

| Método | Rota | Quem pode |
|---|---|---|
| GET | `/health` | anônimo |
| POST | `/api/usuarios` | JWT + `Admin` |
| GET | `/api/usuarios/{id}` | JWT + `Admin,Supervisor,Agente` |
| GET | `/api/usuarios?grupoEmpresaId=` | JWT + `Admin,Supervisor,Agente` |
| POST | `/api/chamados` | JWT (policy `UsuarioPortal`) |
| GET | `/api/chamados` | JWT (fila; cliente vê só o próprio grupo) |
| GET | `/api/chamados/{codigo}` | JWT (cliente vê só públicas; equipe vê notas internas) |
| POST | `/api/chamados/{codigo}/interacoes` | JWT (responder; `Publica` ou `NotaInterna`) |
| PATCH | `/api/chamados/{codigo}/status` | JWT (cliente só reabre `Resolvido→Aberto`) |
| PATCH | `/api/chamados/{codigo}/atribuir` | JWT + `Admin,Supervisor,Agente` (manual ou `automatico: true`) |

Respostas: `201` criou, `400` validação, `401` sem token, `403` sem papel, `404` não achou, `409` e-mail duplicado. Erros vêm em `ProblemDetails`.

## Autenticação (resumo)

### Login manual validado pela API

O painel só abre a sessão após `GET /api/usuarios/me` retornar 200. O endpoint usa o JWT RS256 e o provisionamento existente, retornando `usuarioId`, `nome`, `email`, `papel`, `grupoEmpresaId`, `grupoEmpresaNome`, `expiraEm` e `servidorEm` (horários UTC), com `Cache-Control: no-store`. Não retorna o token.

Uma identidade `Cliente` válida também pode consultar sua própria sessão; o painel interno aceita somente Agente, Supervisor e Admin. API key não substitui a identidade do usuário neste endpoint.

Token inválido, expirado ou com claims obrigatórias ausentes retorna 401. Falha de infraestrutura durante o provisionamento retorna erro de servidor, para não apresentar uma indisponibilidade como token inválido. As demais operações continuam validando JWT e permissões a cada requisição.

Para conferir a configuração efetiva, incluindo as sobrescritas de User Secrets e ambiente, execute em `src/backend/`:

```powershell
dotnet run --project src/SupportAtos.Api -- --check-dev-auth
```

O diagnóstico exige Development, não inicia o servidor nem acessa o banco e não imprime chaves, tokens ou senhas. Ele compara a chave pública efetiva com `tools/SupportAtos.TokenMinter/keys/dev-public.pem` e identifica a fonte de cada configuração. Se houver divergência, atualize o User Secrets e reinicie a API:

```powershell
$pem = Get-Content -LiteralPath "tools/SupportAtos.TokenMinter/keys/dev-public.pem" -Raw
dotnet user-secrets set "Sso:PublicKeyPem" $pem --project src/SupportAtos.Api
```

O emissor não sobrescreve mais um par existente com `generate`. Rotação exige `generate --overwrite`, atualização da chave pública da API e reinício; tokens assinados pelo par anterior deixam de ser aceitos quando a API passa a confiar no novo par.

Testes do pipeline JWT e deste contrato, com provisionamento substituído e sem PostgreSQL:

```powershell
dotnet test tests/SupportAtos.Integration.Tests --filter FullyQualifiedName~SessaoApiTests
```

Esses testes não substituem os testes existentes do provisionamento real com PostgreSQL.

- **JWT RS256 do portal** (`Authorization: Bearer <token>`): valida assinatura + `iss` + `aud` + validade. É o que o frontend e o Swagger usam.
- **API Key** (`X-Api-Key`): só para máquina-a-máquina (ex.: n8n). Não abre rotas de usuário.
- Token de dev: `.\scripts\mint-token.ps1` (usa a chave privada local em `tools/.../keys/`, que **nunca** vai para o git).

## Estrutura de pastas

```
SupportAtos.slnx
├── src/
│   ├── SupportAtos.Domain/           # Regras puras, entidades, enums. Não depende de nada.
│   ├── SupportAtos.Application/      # Casos de uso, DTOs, contratos (Ports). Depende só de Domain.
│   ├── SupportAtos.Infrastructure/   # EF Core, Migrations, repositórios. Depende de Application + Domain.
│   └── SupportAtos.Api/              # Controllers, DI, Swagger, middlewares. Só orquestra.
│       ├── Controllers/              # ChamadosController, UsuariosController
│       ├── Authentication/           # JWT RS256 + API key, leitura de claims, provisioning
│       └── Middleware/               # Traduz exceções para ProblemDetails
├── tools/SupportAtos.TokenMinter/    # Gera keypair e emite/verifica JWT de dev
├── scripts/mint-token.ps1            # Atalho para gerar token e copiar (Ctrl+V)
└── tests/                            # Application.Tests (unidade, sem banco) + Integration.Tests (precisa do Postgres)
```

Regra de ouro da Clean Architecture aqui: Controller **nunca** fala com o banco direto — ele chama um caso de uso, que chama um repositório.

## Segredos (leia antes do primeiro commit)

- **Nunca commitar:** senhas, API keys, connection strings reais, `keys/dev-private.pem`. Tudo já está no `.gitignore`.
- Dev → User Secrets. Outros ambientes → variáveis de ambiente (`ConnectionStrings__DefaultConnection`, `Sso__ApiKey`).
- Testes de integração usam o Postgres do Docker (`5433`) ou `SUPPORTATOS_TEST_CONNECTION`.

## Comandos úteis

```bash
# Nova migration (depois de mexer em entidade/mapeamento)
dotnet ef migrations add NomeDaMigration --project src/SupportAtos.Infrastructure --startup-project src/SupportAtos.Api

# Aplicar migrations
dotnet ef database update --project src/SupportAtos.Infrastructure --startup-project src/SupportAtos.Api

# Build + testes rápidos (sem banco)
dotnet build SupportAtos.slnx
dotnet test tests/SupportAtos.Application.Tests

# Testes de integração (precisa do Postgres na 5433)
dotnet test
```
