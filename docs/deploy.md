# Implantação — Atos Support Hub (Squad 74)

> Documento operacional de implantação da plataforma de suporte **Atos Support Hub**.
> Projeto: Residência em Software — Atos Capital / Squad 74.
> Stack: React + Vite, .NET 10 Web API, PostgreSQL e autenticação SSO/JWT RS256.
> Última atualização: 03/11/2026.

Este documento descreve a arquitetura híbrida de deploy, as variáveis de ambiente, o passo a passo de publicação no Render e na Netlify, a preparação do banco PostgreSQL, a execução dos seeds e as credenciais/identidades de teste para a banca avaliadora.

## Índice

1. [Arquitetura de deploy híbrida](#1-arquitetura-de-deploy-híbrida)
2. [Frontend React/Vite na Netlify](#2-frontend-reactvite-na-netlify)
3. [Backend .NET 10 no Render](#3-backend-net-10-no-render)
4. [Banco de dados PostgreSQL](#4-banco-de-dados-postgresql)
5. [Autenticação SSO/JWT](#5-autenticação-ssojwt)
6. [Usuários e credenciais de teste](#6-usuários-e-credenciais-de-teste)
7. [Checklist de implantação](#7-checklist-de-implantação)
8. [Troubleshooting](#8-troubleshooting)
9. [Segurança e operação](#9-segurança-e-operação)
10. [Referências do repositório](#10-referências-do-repositório)

---

## 1. Arquitetura de deploy híbrida

A solução é dividida em três camadas independentes: frontend estático na Netlify, API containerizada no Render e PostgreSQL gerenciado ou local.

```mermaid
flowchart LR
    U[Usuário / Banca] -->|HTTPS| N[Netlify CDN<br/>React + Vite]
    U -->|JWT via portal SSO| S[Portal SSO<br/>RS256 / JWKS]
    N -->|HTTPS + Authorization: Bearer| R[Render<br/>.NET 10 Web API<br/>Docker]
    R -->|Npgsql + SSL| P[(PostgreSQL<br/>Neon / Supabase / Render / Docker)]
    R -->|Webhooks / X-Api-Key| W[n8n<br/>opcional]
    R -->|JIT provisioning| P
```

### 1.1 URLs do ambiente

| Camada | Provedor | URL | Observação |
|---|---|---|---|
| Frontend | Netlify | <https://cerulean-syrniki-02dc26.netlify.app/> | Build estática do React/Vite |
| API | Render | `https://<servico>.onrender.com` | O blueprint cria o serviço `system-atos-api`; copie a URL final no dashboard |
| API — health check | Render | `https://<servico>.onrender.com/health` | Rota anônima; deve retornar `Healthy` |
| Banco | Neon, Supabase ou Render Postgres | String de conexão privada | Nunca exposta no frontend |
| SSO | Portal Atos Capital | Configurável por `Sso__Issuer`, `Sso__Audience` e `Sso__JwksUrl` | Em homologação pode usar chave PEM de desenvolvimento |

> **Atenção:** a build publicada na Netlify referencia atualmente `https://system-atos-squad74.onrender.com/api` como `VITE_API_URL`. Confirme no dashboard do Render qual é a URL real do serviço Docker antes da avaliação. Se o serviço for recriado, atualize a variável na Netlify e refaça o deploy.

### 1.2 Componentes

| Componente | Tecnologia | Responsabilidade |
|---|---|---|
| Frontend | React 18, TypeScript, Vite 6, MUI | Painel interno do atendente: fila, detalhe do chamado, dashboard |
| API | .NET 10, ASP.NET Core, Clean Architecture | Regras de negócio, autenticação, provisionamento de usuários e endpoints REST |
| Persistência | EF Core 10 + PostgreSQL | Schema versionado por migrations |
| Autenticação | JWT RS256 via SSO do portal | Validação de assinatura, emissor, audiência e validade |
| API key | Header `X-Api-Key` | Integrações máquina-a-máquina, como n8n |
| Frontend hosting | Netlify | CDN, HTTPS e fallback de SPA |
| Backend hosting | Render | Container Docker, variáveis de ambiente e health check |
| Banco gerenciado | Neon / Supabase / Render Postgres | PostgreSQL com backup e disponibilidade gerenciados |

### 1.3 Decisões de arquitetura

- **Frontend estático:** o Vite gera `dist/`, publicado na Netlify. Não há servidor Node em produção.
- **Backend containerizado:** o Render executa a imagem construída pelo `Dockerfile` multi-stage em `src/backend`.
- **Banco desacoplado:** a API usa apenas a connection string; o banco pode estar no Neon, Supabase, Render ou em um container local.
- **Comunicação sem cookies:** o frontend envia `Authorization: Bearer <JWT>`; por isso o CORS pode liberar origens sem credenciais.
- **Provisionamento JIT:** no primeiro acesso autenticado, a API cria ou atualiza o grupo e o usuário a partir das claims do JWT.
- **Migrations como fonte da verdade:** o schema oficial é definido em `src/backend/src/SupportAtos.Infrastructure/Migrations`. Os arquivos `.sql` de `database/` são legado/referência.

---

## 2. Frontend React/Vite na Netlify

### 2.1 Configuração de build

O frontend está em `src/frontend`. A configuração recomendada na Netlify é:

| Configuração | Valor |
|---|---|
| Base directory | `src/frontend` |
| Build command | `npm ci && npm run build` |
| Publish directory | `dist` |
| Node version | 20 LTS (recomendado) |
| SPA fallback | `public/_redirects` com `/* /index.html 200` |

O script `npm run build` executa `tsc -b && vite build`, ou seja, valida os tipos TypeScript e gera a pasta `dist/`.

### 2.2 Variável `VITE_API_URL`

A variável é **incorporada no bundle durante o build**. Alterá-la exige um novo deploy na Netlify.

```env
VITE_API_URL=https://<servico>.onrender.com/api
```

Regras importantes:

- Inclua o sufixo **`/api`**: o cliente HTTP concatena `VITE_API_URL` com rotas como `/usuarios/me` e `/chamados`. Sem `/api`, a requisição vai para o caminho errado.
- Não inclua barra final: evite `.../api/`, pois as rotas já começam com `/`.
- Em desenvolvimento local, `VITE_API_URL` pode ficar ausente: o Vite faz proxy de `/api` para `http://localhost:5177`.

Exemplo correto:

```env
VITE_API_URL=https://system-atos-api.onrender.com/api
```

Exemplo incorreto:

```env
VITE_API_URL=https://system-atos-api.onrender.com
```

> O arquivo `src/frontend/.env.example` mostra apenas o host como exemplo. Para este código, acrescente `/api` ao valor.

### 2.3 Passo a passo na Netlify

1. Acesse o painel da Netlify e selecione o site do projeto.
2. Em **Site configuration → Build & deploy → Environment variables**, crie:
   - `VITE_API_URL` = `https://<servico>.onrender.com/api`
   - Opcionalmente, `NODE_VERSION` = `20`
3. Em **Build & deploy → Continuous deployment**, conecte o repositório GitHub e selecione a branch de publicação (`main` ou a branch de entrega).
4. Configure:
   - **Base directory:** `src/frontend`
   - **Build command:** `npm ci && npm run build`
   - **Publish directory:** `dist`
5. Salve e dispare **Deploy site**.
6. Valide a aplicação em <https://cerulean-syrniki-02dc26.netlify.app/>.
7. Se a variável for alterada, use **Clear cache and deploy site** para garantir que o novo valor seja embutido no bundle.

### 2.4 Fallback de SPA

O arquivo `src/frontend/public/_redirects` contém:

```text
/*    /index.html    200
```

Isso permite acessar diretamente rotas como `/fila`, `/dashboard` e `/ticket/DEMO-0001` sem erro 404 da Netlify.

### 2.5 CORS no backend

O backend lê as origens permitidas da seção `Frontend:Origins`. Para o frontend na Netlify, configure a origem **exata**, sem barra final:

```text
https://cerulean-syrniki-02dc26.netlify.app
```

Em desenvolvimento, mantenha também:

```text
http://localhost:5173
```

No Render, a forma mais segura de configurar múltiplas origens é usar variáveis indexadas:

```text
Frontend__Origins__0=https://cerulean-syrniki-02dc26.netlify.app
Frontend__Origins__1=http://localhost:5173
```

O `render.yaml` declara `Frontend__Origins` como `sync: false`; preencha o valor no dashboard. Se a lista não for reconhecida, a API aplica a política de fallback `AllowAnyOrigin` (adequada para autenticação por Bearer, sem cookies), e o frontend continua funcionando.

---

## 3. Backend .NET 10 no Render

### 3.1 Dockerfile multi-stage

O arquivo `src/backend/Dockerfile` usa duas etapas:

1. **Build:** imagem `mcr.microsoft.com/dotnet/sdk:10.0`, restaura os pacotes NuGet com cache e executa `dotnet publish` em `Release`.
2. **Runtime:** imagem `mcr.microsoft.com/dotnet/aspnet:10.0`, copia apenas os artefatos publicados e executa como usuário não-root (`USER $APP_UID`).

Características relevantes:

- `ASPNETCORE_URLS=http://+:8080` por padrão dentro do container.
- `EXPOSE 8080`.
- `DOTNET_EnableDiagnostics=0` no runtime.
- Entrypoint: `dotnet SupportAtos.Api.dll`.
- O Render pode sobrescrever a porta por `ASPNETCORE_URLS` e pela variável `PORT`.

Build local opcional:

```bash
docker build -t supportatos-api:local ./src/backend
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  supportatos-api:local
```

> A API não sobe sem a configuração de SSO. Para rodar o container localmente, informe `Sso__Issuer`, `Sso__Audience` e `Sso__PublicKeyPem` (ou `Sso__JwksUrl`).

### 3.2 O que o `render.yaml` faz

O arquivo `render.yaml`, na raiz do repositório, declara dois serviços:

| Serviço | Tipo | Root | Função |
|---|---|---|---|
| `system-atos-api` | `web` / `docker` | `src/backend` | API .NET em container |
| `system-atos-squad74-frontend` | `web` / `static` | `src/frontend` | Frontend estático opcional no Render |

Configurações principais da API:

```yaml
services:
  - type: web
    name: system-atos-api
    runtime: docker
    plan: free
    rootDir: src/backend
    dockerfilePath: ./Dockerfile
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ASPNETCORE_URLS
        value: http://0.0.0.0:$PORT
      - key: ConnectionStrings__DefaultConnection
        sync: false
      - key: Sso__PublicKeyPem
        sync: false
      - key: Sso__Issuer
        sync: false
      - key: Sso__Audience
        sync: false
      - key: Sso__ApiKey
        sync: false
      - key: Frontend__Origins
        sync: false
```

`sync: false` significa que o valor **não fica no repositório**: o Render solicita o valor no momento da criação do Blueprint e o armazena como secret no dashboard.

Observações:

- O `render.yaml` **não cria um banco PostgreSQL**. Crie o banco separadamente (Neon, Supabase ou Render Postgres) e informe a connection string.
- O `render.yaml` **não executa migrations automaticamente**. Aplique-as antes de usar a API (ver seção 4.3).
- `ASPNETCORE_URLS` usa `$PORT`, injetada pelo Render em runtime. Se o container não fizer bind na porta esperada, confira a porta indicada no dashboard e ajuste o valor para `http://0.0.0.0:<porta>` (normalmente `10000`) ou para `http://+:8080`, conforme a configuração do serviço.
- O serviço estático do frontend no Render é opcional. Como o frontend oficial está na Netlify, o serviço `system-atos-squad74-frontend` pode ser suspenso ou removido do blueprint para evitar duas publicações do mesmo frontend.
- Recomenda-se adicionar `healthCheckPath: /health` ao serviço da API. O Render usará essa rota para validar o container após o deploy.

### 3.3 Passo a passo para subir a API no Render

1. **Publique o repositório no GitHub** e confirme que `render.yaml` está na raiz da branch selecionada.
2. **Crie o banco PostgreSQL** no provedor escolhido (Neon, Supabase ou Render Postgres) e guarde a connection string.
3. **Aplique as migrations** no banco (seção 4.3). Isso cria as tabelas `usuario`, `grupo_empresa`, `chamado`, `interacao`, `anexo`, `sla_config`, `log_auditoria` e `provisionamento_audit`.
4. No Render, clique em **New + → Blueprint**.
5. Conecte sua conta do GitHub e selecione o repositório do projeto.
6. Selecione a branch de publicação e confirme o caminho `render.yaml`.
7. Clique em **Apply** para criar os serviços.
8. Preencha as variáveis marcadas com `sync: false`:
   - `ConnectionStrings__DefaultConnection`
   - `Sso__PublicKeyPem`
   - `Sso__Issuer`
   - `Sso__Audience`
   - `Sso__ApiKey`
   - `Frontend__Origins`
9. Inicie o deploy e acompanhe os logs em **Logs**.
10. Aguarde o status **Live** e copie a URL pública do serviço (algo como `https://system-atos-api.onrender.com`).
11. Valide o health check:

```bash
curl -i https://<servico>.onrender.com/health
```

Resposta esperada: `HTTP 200` com `Healthy`.

12. Configure a Netlify com `VITE_API_URL=https://<servico>.onrender.com/api` e refaça o deploy do frontend.
13. Se necessário, ajuste `Frontend__Origins` com a URL da Netlify e reinicie o serviço da API.
14. Execute o seed do banco (seção 4.4).

> **Plano free do Render:** o serviço pode “dormir” após um período sem tráfego. A primeira requisição após a inatividade pode levar dezenas de segundos. Para a banca, acesse o frontend com antecedência ou chame `/health` antes da avaliação.

### 3.4 Variáveis de ambiente do backend

#### Obrigatórias

| Variável | Exemplo | Descrição |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Ambiente de execução. Swagger fica desabilitado fora de `Development`. |
| `ASPNETCORE_URLS` | `http://0.0.0.0:$PORT` | Endereço de escuta. O Render injeta `PORT`; o Dockerfile usa `8080` como padrão. |
| `ConnectionStrings__DefaultConnection` | `Host=...;Port=5432;Database=supportatos;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` | Conexão Npgsql com o PostgreSQL. |
| `Sso__Issuer` | `https://portal.atos-capital.example.com` ou `supportatos-dev` | Emissor esperado no claim `iss`. Deve ser idêntico ao token. |
| `Sso__Audience` | `supportatos-api` | Audiência esperada no claim `aud`. |
| `Sso__PublicKeyPem` | `-----BEGIN PUBLIC KEY-----\n...` | Chave pública RSA em PEM. Alternativa ao JWKS. **Não é a chave privada.** |

> Ao configurar `Sso__PublicKeyPem` no dashboard, cole o PEM completo, incluindo as linhas `BEGIN PUBLIC KEY` e `END PUBLIC KEY`, preservando as quebras de linha. Não use aspas ao redor do valor.

#### Alternativa ao PEM: JWKS

| Variável | Exemplo | Descrição |
|---|---|---|
| `Sso__JwksUrl` | `https://portal.atos-capital.example.com/.well-known/jwks.json` | URL do JWKS do portal. Substitui `Sso__PublicKeyPem`. Se usar essa opção, adicione a variável ao `render.yaml` ou ao dashboard. |

A API lança erro na inicialização se **nem** `Sso__JwksUrl` **nem** `Sso__PublicKeyPem` estiverem configurados, ou se `Sso__Issuer`/`Sso__Audience` estiverem vazios.

#### Recomendadas e opcionais

| Variável | Exemplo | Descrição |
|---|---|---|
| `Sso__ApiKey` | valor aleatório longo | Chave aceita no header `X-Api-Key` para webhooks/n8n. Não autentica rotas de usuário. |
| `Frontend__Origins` | `https://cerulean-syrniki-02dc26.netlify.app` | Origem permitida no CORS. Para várias origens, use `Frontend__Origins__0`, `Frontend__Origins__1`, etc. |
| `Sso__ClockSkewSeconds` | `30` | Tolerância de relógio na validação do JWT. |
| `Sso__CacheTtlSeconds` | `60` | Cache do provisionamento de usuário. |
| `Sso__ClaimNames__Sub` | `sub` | Nome da claim de identificação do usuário. |
| `Sso__ClaimNames__Email` | `email` | Nome da claim de e-mail. |
| `Sso__ClaimNames__Nome` | `name` | Nome da claim de nome. |
| `Sso__ClaimNames__GrupoEmpresaId` | `grupo_empresa_id` | Código externo do grupo. |
| `Sso__ClaimNames__GrupoEmpresaNome` | `grupo_empresa_nome` | Nome do grupo. |
| `Sso__ClaimNames__Role` | `role` | Papel do usuário (`Cliente`, `Agente`, `Supervisor`, `Admin`). |
| `DOTNET_EnableDiagnostics` | `0` | Já definido no Dockerfile. |

#### Exemplo de connection string

Para Neon/Supabase (SSL obrigatório):

```text
Host=ep-exemplo.us-east-2.aws.neon.tech;Port=5432;Database=supportatos;Username=usuario;Password=senha;SSL Mode=Require;Trust Server Certificate=true
```

Para Render Postgres (rede interna, mesma região):

```text
Host=dpg-xxxxxxxxxxxx-a;Port=5432;Database=supportatos;Username=usuario;Password=senha
```

Para Docker local:

```text
Host=localhost;Port=5433;Database=supportatos;Username=admin;Password=supportatos
```

> Em migrations, prefira a conexão **direta** do provedor. Se usar pooler (Neon/Supabase), valide a compatibilidade com o Npgsql e reserve a conexão direta para operações de schema.

---

## 4. Banco de dados PostgreSQL

### 4.1 Opções de hospedagem

| Opção | Vantagens | Pontos de atenção |
|---|---|---|
| **Neon** | Serverless, criação rápida, bom para homologação | SSL obrigatório; pode suspender por inatividade; use conexão direta para migrations |
| **Supabase** | Banco gerenciado com painel e SQL Editor | SSL obrigatório; pooler pode usar porta 6543; migrations pela conexão direta |
| **Render Postgres** | Integração com o Render; conexão interna simples | Disponível apenas na mesma região; planos gratuitos podem ter limitações |
| **Docker local** | Controle total, sem custo, bom para desenvolvimento | Não é adequado para produção; volumes precisam ser persistidos |

### 4.2 Criar o banco

O nome sugerido é `supportatos`.

**Neon/Supabase:** crie o projeto e copie a connection string. Converta a URI para o formato Npgsql aceito pela API, ou use a URI diretamente com `psql` para os scripts.

```bash
export DATABASE_URL="postgresql://usuario:senha@host:5432/supportatos?sslmode=require"
psql "$DATABASE_URL" -c "select version();"
```

**Render Postgres:** crie o banco no dashboard e copie a **Internal Database URL** (ou a External URL, se for acessar de fora).

**Docker local:**

```bash
docker compose -f src/backend/docker-compose.yml up -d
```

O compose sobe PostgreSQL 17 em `localhost:5433`, usuário `admin`, senha padrão `supportatos` e banco `supportatos`.

### 4.3 Aplicar as migrations

As migrations são a fonte oficial do schema. O arquivo `database/atos_capital_postgresql.sql` é legado e **não deve ser aplicado junto com as migrations**.

Instale a ferramenta do EF Core, se necessário:

```bash
dotnet tool install --global dotnet-ef
```

Execute a partir de `src/backend`:

**PowerShell:**

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=...;Port=5432;Database=supportatos;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"

dotnet ef database update `
  --project src/SupportAtos.Infrastructure `
  --startup-project src/SupportAtos.Api
```

**Bash:**

```bash
cd src/backend
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=supportatos;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true" \
dotnet ef database update \
  --project src/SupportAtos.Infrastructure \
  --startup-project src/SupportAtos.Api
```

> `ASPNETCORE_ENVIRONMENT=Development` garante que a chave pública de desenvolvimento de `appsettings.Development.json` esteja disponível durante o design-time do EF. Se preferir rodar em `Production`, informe também `Sso__PublicKeyPem` ou `Sso__JwksUrl`, pois a API valida a configuração de SSO na inicialização.

Confirme as tabelas:

```bash
psql "$DATABASE_URL" -c "\dt"
psql "$DATABASE_URL" -c "select * from \"__EFMigrationsHistory\";"
```

### 4.4 Executar o seed

#### Seed oficial do schema atual

O arquivo `src/backend/database/seed.sql` insere:

- um grupo de empresa (`Empresa Demo`);
- três configurações de SLA;
- o usuário `Cliente Demo` (`cliente@demo.com`).

Execute **após** as migrations:

```bash
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f src/backend/database/seed.sql
```

No Windows, se o `psql` estiver no PATH:

```powershell
psql -h <host> -p 5432 -U <usuario> -d supportatos `
  -v ON_ERROR_STOP=1 `
  -f src/backend/database/seed.sql
```

Também é possível abrir o arquivo no SQL Editor do Neon/Supabase e executar o conteúdo.

> O seed usa `codigo_externo` para o grupo e `ON CONFLICT` para SLA e usuário, portanto pode ser reexecutado com segurança na maioria dos casos. Ainda assim, revise os dados antes de rodá-lo em uma base já populada.

#### Script legado/referência

O arquivo `database/atos_capital_postgresql.sql` contém o schema e os dados iniciais completos da primeira versão, incluindo os usuários de teste. Ele **remove as tabelas existentes** (`DROP TABLE ... CASCADE`), usa nomes/estruturas que divergem do modelo atual e não possui as colunas de `codigo_externo` usadas pelo provisionamento JIT.

Use-o apenas em um banco isolado de demonstração, nunca no banco da API que já recebeu migrations:

```bash
psql "$DATABASE_URL_LEGADO" -v ON_ERROR_STOP=1 -f database/atos_capital_postgresql.sql
```

#### Criar/atualizar os cinco usuários de teste no schema atual

Para a banca, os usuários das personas podem ser criados no schema atual com o bloco abaixo. Execute **depois** de `seed.sql` e das migrations:

```sql
-- Grupos de demonstração
INSERT INTO grupo_empresa (codigo_externo, nome, tipo)
VALUES
  ('grupo-atos-matriz', 'Atos Capital - Matriz', 'matriz'),
  ('grupo-filial-recife', 'Filial Recife', 'filial'),
  ('grupo-filial-sp', 'Filial São Paulo', 'filial')
ON CONFLICT (codigo_externo)
DO UPDATE SET nome = EXCLUDED.nome;

-- Usuários de teste
INSERT INTO usuario (codigo_externo, nome, email, grupo_empresa_id, papel, criado_em)
SELECT
  v.codigo_externo,
  v.nome,
  v.email,
  g.id,
  v.papel,
  now()
FROM (VALUES
  ('portal-atos',  'Atos',         'atos@gmail.com',              'grupo-atos-matriz',  'Admin'),
  ('portal-maria', 'Maria Silva',  'maria.silva@cliente.com',     'grupo-atos-matriz',  'Cliente'),
  ('portal-joao',  'João Pereira', 'joao.pereira@cliente.com',    'grupo-filial-recife', 'Cliente'),
  ('portal-ana',   'Ana Costa',    'ana.costa@cliente.com',       'grupo-filial-sp',    'Cliente'),
  ('portal-demo',  'Cliente Demo', 'cliente@demo.com',            'grupo-atos-matriz',  'Cliente')
) AS v(codigo_externo, nome, email, grupo_codigo, papel)
JOIN grupo_empresa g ON g.codigo_externo = v.grupo_codigo
ON CONFLICT (email) DO UPDATE
SET codigo_externo    = EXCLUDED.codigo_externo,
    nome              = EXCLUDED.nome,
    grupo_empresa_id  = EXCLUDED.grupo_empresa_id,
    papel             = EXCLUDED.papel,
    atualizado_em     = now();
```

> O `papel` técnico do Atos é `Admin`. O rótulo de negócio “Gerente” aparece no script legado; a API atual aceita apenas os papéis do enum `Cliente`, `Agente`, `Supervisor` e `Admin`.

Valide os registros:

```bash
psql "$DATABASE_URL" -c "SELECT id, nome, email, papel, grupo_empresa_id FROM usuario ORDER BY id;"
```

### 4.5 Observação sobre provisionamento JIT

No primeiro login com JWT, a API procura o usuário e o grupo por `codigo_externo` (claims `sub` e `grupo_empresa_id`). Se não encontrar, cria os registros. Portanto:

- o `sub` do token deve ser estável e exclusivo;
- para atualizar um usuário já seedado, prefira alinhar o `codigo_externo` ao `sub` usado no token;
- se existir um usuário com o mesmo e-mail, mas sem `codigo_externo`, o provisionamento pode falhar por conflito de e-mail. O bloco SQL acima já preenche `codigo_externo` para os usuários de teste.

---

## 5. Autenticação SSO/JWT

### 5.1 Como funciona

A API usa **JWT RS256** emitido pelo portal/SSO:

- valida a assinatura com chave pública PEM ou JWKS;
- valida `iss` (emissor), `aud` (audiência), `exp`/`nbf` e algoritmo `RS256`;
- exige as claims `sub`, `email`, `name`, `grupo_empresa_id`, `grupo_empresa_nome` e `role`;
- no evento `OnTokenValidated`, executa o provisionamento JIT do grupo e do usuário;
- o painel interno chama `GET /api/usuarios/me` para validar a sessão e obter nome, papel, grupo e validade.

### 5.2 Claims esperadas

| Claim | Exemplo | Uso |
|---|---|---|
| `sub` | `portal-atos` | Identificador externo do usuário |
| `email` | `atos@gmail.com` | E-mail do usuário |
| `name` | `Atos` | Nome exibido |
| `grupo_empresa_id` | `grupo-atos-matriz` | Código externo do grupo |
| `grupo_empresa_nome` | `Atos Capital - Matriz` | Nome do grupo |
| `role` | `Admin` | Papel: `Cliente`, `Agente`, `Supervisor` ou `Admin` |
| `iss` | `supportatos-dev` | Emissor esperado |
| `aud` | `supportatos-api` | Audiência esperada |
| `exp` | timestamp | Expiração |
| `nbf` | timestamp | Início da validade |

### 5.3 Papéis e permissões

| Papel | Acesso |
|---|---|
| `Cliente` | Abre e acompanha chamados do próprio grupo; pode reabrir chamado resolvido |
| `Agente` | Fila, detalhe, respostas, notas internas, status e atribuição |
| `Supervisor` | Acesso de agente com visão ampliada |
| `Admin` | Acesso administrativo, incluindo cadastro de usuários |

O painel React é destinado à **equipe interna**. Tokens de `Cliente` são válidos para a API, mas o frontend exibe a mensagem de que o painel é apenas para Agente/Supervisor/Admin.

### 5.4 Login na interface

A tela `/login` recebe um JWT colado manualmente:

- aceita o token com ou sem o prefixo `Bearer`;
- valida em `GET /api/usuarios/me` antes de abrir a sessão;
- mantém o token **somente em memória** (não usa `localStorage`);
- após F5, é necessário colar o token novamente;
- não há renovação automática; o token padrão do script de desenvolvimento expira em 60 minutos.

### 5.5 API key para integrações

O header `X-Api-Key` permite chamadas máquina-a-máquina, por exemplo do n8n para webhooks. Essa chave:

- não substitui a identidade do usuário;
- não abre rotas de portal;
- deve ser configurada em `Sso__ApiKey` e enviada apenas em endpoints de integração.

### 5.6 Homologação versus produção

| Cenário | Configuração |
|---|---|
| **Homologação/banca** | API com `Sso__PublicKeyPem` da chave de desenvolvimento e `Sso__Issuer=supportatos-dev`; tokens gerados com `mint-token.ps1` |
| **Produção** | API com `Sso__JwksUrl` do portal e `Sso__Issuer`/`Sso__Audience` reais; tokens emitidos pelo SSO corporativo |

> Se a API estiver em produção com a chave/JWKS do portal, um token gerado localmente pelo `mint-token.ps1` retornará `401`. Para a banca, use o ambiente de homologação com a chave pública de desenvolvimento ou obtenha um token do portal.

---

## 6. Usuários e credenciais de teste

> **Não existe senha local no sistema.** A autenticação é feita por SSO/JWT. A coluna “Senha” indica “não se aplica” para todas as identidades. O que funciona como credencial é o **e-mail + token JWT** com as claims correspondentes.

| Nome | E-mail | Senha | Perfil | Grupo | `sub` sugerido | Origem do dado | Acesso esperado |
|---|---|---|---|---|---|---|---|
| **Atos** | `atos@gmail.com` | Não se aplica (JWT/SSO) | **Gerente / Admin** | Atos Capital - Matriz | `portal-atos` | `database/atos_capital_postgresql.sql` e bloco SQL da seção 4.4 | Painel interno completo, dashboard e administração |
| **Maria Silva** | `maria.silva@cliente.com` | Não se aplica (JWT/SSO) | Cliente | Atos Capital - Matriz | `portal-maria` | `database/atos_capital_postgresql.sql` e bloco SQL da seção 4.4 | API/portal do cliente; não entra no painel interno |
| **João Pereira** | `joao.pereira@cliente.com` | Não se aplica (JWT/SSO) | Cliente | Filial Recife | `portal-joao` | `database/atos_capital_postgresql.sql` e bloco SQL da seção 4.4 | API/portal do cliente; não entra no painel interno |
| **Ana Costa** | `ana.costa@cliente.com` | Não se aplica (JWT/SSO) | Cliente | Filial São Paulo | `portal-ana` | `database/atos_capital_postgresql.sql` e bloco SQL da seção 4.4 | API/portal do cliente; não entra no painel interno |
| **Cliente Demo** | `cliente@demo.com` | Não se aplica (JWT/SSO) | Cliente | Empresa Demo / Atos Capital - Matriz | `portal-demo` | `src/backend/database/seed.sql` e bloco SQL da seção 4.4 | API/portal do cliente; não entra no painel interno |

### 6.1 Senhas

| Usuário | Senha |
|---|---|
| Todos os usuários acima | **Não se aplica** — autenticação via SSO/JWT |

Não há senha padrão, hash de senha ou formulário de login por senha no projeto. Qualquer orientação que peça “senha” para a banca deve ser substituída pela geração de um JWT.

### 6.2 Gerar tokens para a banca

Pré-requisitos:

- .NET SDK 10 instalado;
- chave privada de desenvolvimento em `src/backend/tools/SupportAtos.TokenMinter/keys/dev-private.pem`;
- a API de homologação configurada com a chave pública correspondente em `Sso__PublicKeyPem`;
- `Sso__Issuer` e `Sso__Audience` iguais aos usados na geração.

**Atos (Admin/Gerente), no PowerShell:**

```powershell
cd src/backend
.\scripts\mint-token.ps1 `
  -Sub portal-atos `
  -Email atos@gmail.com `
  -Name "Atos" `
  -GrupoId grupo-atos-matriz `
  -GrupoNome "Atos Capital - Matriz" `
  -Admin `
  -Minutes 120
```

**Maria (Cliente), no PowerShell:**

```powershell
cd src/backend
.\scripts\mint-token.ps1 `
  -Sub portal-maria `
  -Email maria.silva@cliente.com `
  -Name "Maria Silva" `
  -GrupoId grupo-atos-matriz `
  -GrupoNome "Atos Capital - Matriz" `
  -Papel Cliente `
  -Minutes 120
```

**João (Cliente):**

```powershell
cd src/backend
.\scripts\mint-token.ps1 `
  -Sub portal-joao `
  -Email joao.pereira@cliente.com `
  -Name "João Pereira" `
  -GrupoId grupo-filial-recife `
  -GrupoNome "Filial Recife" `
  -Papel Cliente `
  -Minutes 120
```

**Ana (Cliente):**

```powershell
cd src/backend
.\scripts\mint-token.ps1 `
  -Sub portal-ana `
  -Email ana.costa@cliente.com `
  -Name "Ana Costa" `
  -GrupoId grupo-filial-sp `
  -GrupoNome "Filial São Paulo" `
  -Papel Cliente `
  -Minutes 120
```

**Cliente Demo:**

```powershell
cd src/backend
.\scripts\mint-token.ps1 `
  -Sub portal-demo `
  -Email cliente@demo.com `
  -Name "Cliente Demo" `
  -GrupoId grupo-atos-matriz `
  -GrupoNome "Atos Capital - Matriz" `
  -Papel Cliente `
  -Minutes 120
```

**Alternativa multiplataforma (sem PowerShell):**

```bash
cd src/backend
dotnet run --project tools/SupportAtos.TokenMinter -- token \
  --sub portal-atos \
  --email atos@gmail.com \
  --name "Atos" \
  --grupo-id grupo-atos-matriz \
  --grupo-nome "Atos Capital - Matriz" \
  --papel Admin \
  --issuer supportatos-dev \
  --audience supportatos-api \
  --minutes 120
```

Se o Render estiver configurado com outro emissor/audiência, acrescente ao script:

```powershell
-Issuer "https://portal.atos-capital.example.com" -Audience "supportatos-api"
```

### 6.3 Roteiro de avaliação

1. Abra <https://cerulean-syrniki-02dc26.netlify.app/>.
2. Gere o token do Atos com `-Admin` e cole em `/login`.
3. Confirme nome, grupo e papel exibidos; acesse **Fila** e **Dashboard**.
4. Para testar um cliente, gere o token correspondente e use a API/portal do cliente. O painel interno rejeitará o token de `Cliente`, o que é o comportamento esperado.
5. Se a base estiver vazia, execute o seed e recarregue a fila.
6. Se a primeira requisição demorar, aguarde o cold start do Render e tente novamente.

---

## 7. Checklist de implantação

- [ ] Banco PostgreSQL criado e acessível.
- [ ] Migrations aplicadas (`__EFMigrationsHistory` presente).
- [ ] Seed executado (ou usuários criados pelo bloco SQL da seção 4.4).
- [ ] Blueprint `render.yaml` aplicado no Render.
- [ ] Variáveis `sync: false` preenchidas no dashboard.
- [ ] `Sso__PublicKeyPem` ou `Sso__JwksUrl` configurado.
- [ ] `Sso__Issuer` e `Sso__Audience` iguais aos do token.
- [ ] `Frontend__Origins` contém a URL exata da Netlify.
- [ ] `GET /health` retorna `Healthy`.
- [ ] `VITE_API_URL` na Netlify inclui `/api` e não tem barra final.
- [ ] Deploy da Netlify concluído após a alteração de `VITE_API_URL`.
- [ ] Token do Atos válido no painel.
- [ ] Fila e dashboard carregando dados reais.
- [ ] Logs do Render sem erros de conexão/SSO.

---

## 8. Troubleshooting

| Sintoma | Causa provável | Correção |
|---|---|---|
| `502`/`503` na primeira chamada | Serviço free do Render hibernando | Aguarde o cold start e chame `/health` novamente |
| Container encerra ao iniciar | SSO sem `PublicKeyPem`/`JwksUrl`, ou `Issuer`/`Audience` vazios | Preencha as variáveis de SSO no Render |
| `401 Unauthorized` no login | Token expirado, assinatura/chave diferente ou `iss`/`aud` divergente | Gere novo token com os mesmos valores configurados na API |
| `403 Forbidden` | Papel insuficiente | Use token `Admin`, `Supervisor` ou `Agente` para o painel |
| Cliente não entra no painel | O painel é interno | Use o portal/API do cliente; comportamento esperado |
| `500` com “relation does not exist” | Migrations não aplicadas | Execute `dotnet ef database update` no banco correto |
| `500` com duplicate key | Conflito de e-mail/`codigo_externo` no provisionamento | Alinhe `codigo_externo` ao `sub` do token ou limpe o registro duplicado |
| Erro de CORS no navegador | Origem não configurada | Ajuste `Frontend__Origins` com a URL exata da Netlify, sem `/` final |
| Frontend chama `/usuarios/me` em vez de `/api/usuarios/me` | `VITE_API_URL` sem `/api` | Corrija a variável e refaça o build |
| Refresh em `/fila` retorna 404 | Fallback de SPA ausente | Confirme `public/_redirects` e o publish `dist` |
| Swagger retorna 404 em produção | Swagger é habilitado apenas em `Development` | Use `/health` e o frontend; para Swagger, suba localmente |
| Erro de SSL no PostgreSQL | Provedor exige SSL | Acrescente `SSL Mode=Require;Trust Server Certificate=true` |
| Container não faz bind na porta | `ASPNETCORE_URLS`/`PORT` incompatíveis | Verifique a porta exibida no Render e ajuste `ASPNETCORE_URLS` |
| Erro de conexão com banco interno do Render | Serviço e banco em regiões diferentes | Use a External Database URL ou mova para a mesma região |

---

## 9. Segurança e operação

### 9.1 Segredos

- Nunca commitar `dev-private.pem`, senhas, connection strings reais ou `Sso__ApiKey`.
- Usar `sync: false` no `render.yaml` e o cofre de variáveis do Render/Netlify.
- A chave pública PEM pode ficar em configuração, mas a chave privada correspondente nunca deve ir para o repositório.
- Rotacionar a API key e as chaves de desenvolvimento ao encerrar a residência.

### 9.2 Banco

- Usar SSL em provedores gerenciados.
- Aplicar migrations com a conexão direta, não pelo pooler de transação.
- Manter backup/rotina de restore do provedor.
- Não reutilizar o banco de produção para testes destrutivos ou para o script legado.

### 9.3 Aplicação

- Manter `ASPNETCORE_ENVIRONMENT=Production` no Render.
- Não habilitar Swagger em produção sem controle de acesso.
- Restringir `Frontend__Origins` à URL oficial da Netlify quando possível.
- Monitorar logs do Render e o endpoint `/health`.
- Considerar o cold start do plano free ao planejar a avaliação.

### 9.4 Rollback

| Camada | Procedimento |
|---|---|
| Render | Dashboard → serviço → **Deploys** → selecionar deploy anterior → **Redeploy** |
| Netlify | **Deploys** → selecionar deploy anterior → **Publish deploy** |
| Banco | Restaurar backup/snapshot do provedor; migrations de rollback devem ser avaliadas antes |

---

## 10. Referências do repositório

| Arquivo | Conteúdo |
|---|---|
| `render.yaml` | Blueprint dos serviços Render |
| `src/backend/Dockerfile` | Build multi-stage da API .NET 10 |
| `src/backend/docker-compose.yml` | PostgreSQL local na porta 5433 |
| `src/backend/src/SupportAtos.Api/Program.cs` | CORS, health check, autenticação e pipeline |
| `src/backend/src/SupportAtos.Api/Authentication/AuthenticationSetup.cs` | JWT RS256, JWKS/PEM, API key e provisionamento |
| `src/backend/src/SupportAtos.Infrastructure/Migrations/` | Schema oficial do banco |
| `src/backend/database/seed.sql` | Seed do schema atual |
| `database/atos_capital_postgresql.sql` | Script legado/referência com dados iniciais |
| `src/backend/scripts/mint-token.ps1` | Geração de JWT de desenvolvimento |
| `src/frontend/src/api/client.ts` | Uso de `VITE_API_URL` e `Authorization: Bearer` |
| `src/frontend/public/_redirects` | Fallback de SPA na Netlify |

---

**Documento elaborado para a entrega do Squad 74 — Atos Capital.**
