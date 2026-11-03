# Atos Support Hub - Squad 74

Plataforma de suporte técnico da **Atos Capital** para abertura, acompanhamento e resolução de chamados, com painel interno de atendimento, ciclo de vida completo, SLA e autenticação via SSO/JWT.

**Frontend em produção:** https://inquisitive-torte-0fd4c0.netlify.app/

---

## 🚀 Deploy & Acesso ao Sistema

| Ambiente | URL |
|---|---|
| **Frontend (Netlify)** | **https://inquisitive-torte-0fd4c0.netlify.app/** |
| **Backend (Render)** | URL pública do serviço `system-atos-api` no Render (ex.: `https://system-atos-api.onrender.com`) |
| **Health check** | `https://<API_URL>/health` |

O frontend é uma **SPA React** publicada na Netlify. A API é uma **ASP.NET Core 10 Web API** conteinerizada com Docker e provisionada no Render pelo blueprint [`render.yaml`](render.yaml). O banco é **PostgreSQL 17** gerenciado.

### Usuários de teste para avaliação

O sistema usa **SSO/JWT**, sem senha local armazenada. As identidades abaixo estão semeadas no banco e os tokens de teste são fornecidos pela equipe.

| Nome | E-mail | Perfil | Papel no JWT/API | Grupo / Empresa |
|---|---|---|---|---|
| **Atos** | `atos@gmail.com` | **Gerente / Suporte** | `Admin` | Atos Capital - Matriz |
| **Maria Silva** | `maria.silva@cliente.com` | Cliente | `Cliente` | Atos Capital - Matriz |
| **João Pereira** | `joao.pereira@cliente.com` | Cliente | `Cliente` | Filial Recife |
| **Ana Costa** | `ana.costa@cliente.com` | Cliente | `Cliente` | Filial São Paulo |
| **Cliente Demo** | `cliente@demo.com` | Cliente | `Cliente` | Atos Capital - Matriz |

- O painel da Netlify é o **painel interno** e aceita `Agente`, `Supervisor` e `Admin`.
- O perfil de negócio **Gerente** equivale ao papel técnico **`Admin`** no enum atual da API.
- Para acessar: abra `/login` e cole o JWT de teste no campo **JWT (Bearer)**.

### Documentação de deploy

O passo a passo completo está em **[docs/deploy.md](docs/deploy.md)**, incluindo:

- arquitetura de implantação e variáveis de ambiente;
- deploy no Render com `render.yaml` e publicação na Netlify;
- inicialização do PostgreSQL e seeds (`database/atos_capital_postgresql.sql`);
- credenciais de teste, smoke tests e troubleshooting.

---

## 🐳 Como rodar localmente com Docker Compose

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) com Docker Compose
- Ferramenta EF Core (uma única vez): `dotnet tool install --global dotnet-ef`

### 1. Suba o PostgreSQL

```bash
docker compose -f src/backend/docker-compose.yml up -d
```

O banco fica disponível em `localhost:5433` com:

- usuário: `admin`
- senha: `supportatos`
- database: `supportatos`

> O compose do repositório sobe o **PostgreSQL 17**. A API e o frontend rodam com .NET e Node nos passos seguintes.

### 2. Configure a API e aplique as migrations

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=supportatos;Username=admin;Password=supportatos"

cd src/backend
dotnet ef database update \
  --project src/SupportAtos.Infrastructure \
  --startup-project src/SupportAtos.Api
```

### 3. Rode a API

```bash
cd src/backend
dotnet run --project src/SupportAtos.Api
```

- API: `http://localhost:5177`
- Health check: `http://localhost:5177/health`
- Swagger (somente Development): `http://localhost:5177/swagger`

### 4. Rode o frontend

Em outro terminal:

```bash
cd src/frontend
npm ci
npm run dev
```

Acesse **http://localhost:5173**. Em desenvolvimento, o Vite faz proxy de `/api` para `http://localhost:5177`; não é necessário criar `.env`.

### 5. Gere um token de teste local

```powershell
cd src/backend
.\scripts\mint-token.ps1 -Sub atos -Email atos@gmail.com -Name "Atos" `
  -GrupoId atos-matriz -GrupoNome "Atos Capital - Matriz" -Admin
```

Cole o token no campo **JWT (Bearer)** da tela de login. O detalhamento do ambiente local está em [src/backend/README.md](src/backend/README.md) e [src/frontend/README.md](src/frontend/README.md).

---

## 🧱 Stack

| Camada | Tecnologias |
|---|---|
| Frontend | React 18, TypeScript, Vite, Material UI, React Router, Vitest |
| Backend | C# / .NET 10, ASP.NET Core Web API, Clean Architecture, EF Core 10 |
| Banco | PostgreSQL 17, migrations e seeds |
| Autenticação | JWT RS256 (SSO) + API Key para integrações |
| Infraestrutura | Docker multi-stage, Render Blueprint, Netlify |
| Documentação | Markdown, UML, requisitos e guia de deploy |

## ✨ Principais recursos

- Abertura e acompanhamento de chamados com código público.
- Fila de atendimento com filtros, atribuição manual/automática e polling.
- Ciclo de vida do chamado: `Aberto → Em andamento → Aguardando cliente → Resolvido → Fechado`.
- Interações públicas e notas internas.
- SLA por categoria e prioridade, com indicadores de prazo.
- Dashboard com contadores e visão gerencial.
- Controle de acesso por papel (`Cliente`, `Agente`, `Supervisor`, `Admin`).
- Provisionamento automático de usuários e grupos a partir do SSO/JWT.

## 📁 Estrutura do repositório

```text
.
├── database/                 # Dump/seed legado do PostgreSQL
├── docs/                     # Documentação acadêmica e guia de deploy
│   └── deploy.md             # Deploy, variáveis, banco e acesso de teste
├── src/
│   ├── backend/              # API ASP.NET Core 10, Dockerfile e docker-compose
│   └── frontend/             # SPA React/Vite publicada na Netlify
├── render.yaml               # Blueprint do Render
└── README.md
```

## 📚 Documentação

- [Documentação geral do projeto](docs/README.md)
- [Deploy e acesso ao sistema](docs/deploy.md)
- [Visão geral e requisitos](docs/01-visao-geral.md)
- [Requisitos do sistema](docs/02-requisitos.md)
- [Arquitetura e dados](docs/03-arquitetura-dados.md)
- [Implementação e testes](docs/04-implementacao-testes.md)

## 👥 Equipe — Squad 74

João Pedro Alves Seixas; José Henrique Maciel Dantas; Vinícius A. Sampaio; Anthony Santos Costa; Kayone Silva do Nascimento; Danilo Sá Almeida; Marcus Lucas Dantas da Silva; Pedro Henrique Nunes de Melo.

**Aracaju – 2026 | Residência de Software — UNIT**
