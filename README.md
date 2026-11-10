# SupportAtos

Sistema de suporte com portal do cliente e painel de atendimento.

Stack:

- Backend: C# / .NET 10, ASP.NET Core, EF Core e PostgreSQL
- Frontend: React 18, Vite e Material UI
- Banco: PostgreSQL (nativo ou Docker)

## Aviso importante

> A autenticacao esta temporariamente desativada para facilitar os testes locais.
>
> Procure por `AUTH-DESATIVADA` no codigo para encontrar todos os pontos alterados.
>
> Use este projeto somente em ambiente local. Nao exponha esta configuracao na internet.

## Pre-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) com `npm`
- PostgreSQL instalado ou Docker
- Ferramenta do EF Core (instale uma vez):

```powershell
dotnet tool install --global dotnet-ef
```

Se o comando `dotnet ef` nao for encontrado, feche e abra o terminal novamente.

## 1. Banco de dados

Escolha uma das opcoes abaixo.

### Opcao A - PostgreSQL nativo

Crie a base:

```powershell
psql -h localhost -p 5432 -U postgres -c "CREATE DATABASE supportatos;"
```

Configure a conexao fora do Git usando User Secrets:

```powershell
cd src\backend\src\SupportAtos.Api

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=supportatos;Username=postgres;Password=SUA_SENHA"
```

### Opcao B - PostgreSQL com Docker

O compose do backend sobe o PostgreSQL na porta `5433`:

```powershell
cd src\backend
docker compose up -d
```

Credenciais do container:

- Usuario: `admin`
- Senha: `supportatos`
- Banco: `supportatos`
- Porta no host: `5433`

Configure a conexao:

```powershell
cd src\backend\src\SupportAtos.Api

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=supportatos;Username=admin;Password=supportatos"
```

### Aplicar migrations

A partir de `src\backend`:

```powershell
dotnet ef database update --project src/SupportAtos.Infrastructure --startup-project src/SupportAtos.Api
```

### Aplicar seed

Ainda em `src\backend`, com o PostgreSQL nativo:

```powershell
psql -h localhost -p 5432 -U postgres -d supportatos -f database/seed.sql
```

Se estiver usando Docker, troque a porta e o usuario:

```powershell
psql -h localhost -p 5433 -U admin -d supportatos -f database/seed.sql
```

O `seed.sql` cria o minimo para testar a API: um grupo empresa, tres configuracoes de SLA e um usuario cliente.

Como o backend e o frontend usam um usuario mock com ID `4` e grupo empresa `3`, aplique tambem o seed do mock:

```powershell
psql -h localhost -p 5432 -U postgres -d supportatos -f database/seed_mock.sql
```

No Docker, use `-p 5433 -U admin`.

O arquivo `database/seed_mock.sql` e idempotente e cria o grupo `Grupo Norte` e o usuario `Admin` usados pela sessao mock.

Existe tambem o arquivo `database/seed_chamados_exemplo.sql`, com chamados e interacoes de exemplo. Antes de usa-lo, confira os pre-requisitos descritos no topo do proprio arquivo, pois ele assume usuarios e SLAs com IDs especificos.

> Nao aplique `database/atos_capital_postgresql.sql` junto com as migrations. Ele e um arquivo legado de referencia.

## 2. Backend

```powershell
cd src\backend\src\SupportAtos.Api
dotnet run --launch-profile http
```

A API sobe em:

- API: `http://localhost:5177`
- Health check: `http://localhost:5177/health`
- Swagger: `http://localhost:5177/swagger`

### Se o Windows bloquear o .exe

Algumas maquinas com politica de Controle de Aplicativo bloqueiam o executavel gerado pelo `dotnet run`. Nesse caso, compile e rode a DLL:

```powershell
cd src\backend\src\SupportAtos.Api

dotnet build

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5177"

dotnet bin\Debug\net10.0\SupportAtos.Api.dll
```

As variaveis de ambiente sao necessarias ao rodar a DLL diretamente, porque o `launchSettings.json` nao e aplicado.

## 3. Frontend

Em outro terminal:

```powershell
cd src\frontend
npm install
npm run dev
```

Abra:

```text
http://localhost:5173
```

O Vite encaminha chamadas de `/api` para `http://localhost:5177`.

Opcionalmente, crie um `.env` a partir do exemplo:

```powershell
Copy-Item .env.example .env
```

Conteudo esperado:

```env
VITE_API_URL=/api
VITE_PROXY_TARGET=http://localhost:5177
```

O arquivo `.env` e ignorado pelo Git e nao deve ser commitado.

## 4. Teste rapido

1. Abra `http://localhost:5173`.
2. A aplicacao deve abrir direto, sem tela de login, usando a sessao mock.
3. Confirme que a fila de chamados carrega dados vindos da API.
4. Abra um chamado e teste:
   - envio de resposta ao cliente;
   - nota interna;
   - alteracao de status;
   - assumir ou repassar o chamado.
5. Abra o DevTools do navegador, aba Network, e confirme as chamadas para `/api/chamados` e `/api/usuarios/...`.

### Criar um chamado de teste via API

Como a sessao mock atual e de agente, o botao de novo chamado nao aparece na interface. Para popular a fila e testar o fluxo completo, crie um chamado pela API:

```powershell
$body = @{
  produto = "Portal"
  categoria = "Login"
  titulo = "Chamado de teste"
  descricao = "Criado via API para validar o fluxo."
  mensagemInicial = "Criado via API para validar o fluxo."
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "http://localhost:5177/api/chamados" `
  -ContentType "application/json" -Body $body
```

Depois atualize a pagina em `http://localhost:5173`.

### Sobre os dados

- Requisicoes `GET` sao seguras para consulta.
- Requisicoes `POST` e `PATCH` alteram o banco.
- O backend usa um usuario mock com ID `4` e grupo empresa `3`.
- Em um banco novo, esse usuario e grupo precisam existir para testar criacao e atribuicao de chamados.

## 5. Problemas comuns

### O backend nao sobe e o Windows bloqueia o .exe

Use o fallback com `dotnet bin\Debug\net10.0\SupportAtos.Api.dll`, conforme mostrado acima.

### Erro de conexao com o PostgreSQL

Confira:

- se o PostgreSQL esta rodando;
- se a porta e o usuario estao corretos (`5432/postgres` no nativo ou `5433/admin` no Docker);
- se a connection string foi configurada via User Secrets.

Teste a API:

```powershell
Invoke-RestMethod http://localhost:5177/health
Invoke-RestMethod http://localhost:5177/api/chamados
```

### A fila aparece vazia

Pode ser que o seed nao tenha sido aplicado ou que o banco nao tenha chamados. Verifique:

```powershell
Invoke-RestMethod http://localhost:5177/api/chamados
```

### Erro de CORS

O backend libera `http://localhost:5173` e `http://127.0.0.1:5173`. Se voce usar outra porta, ajuste `Cors:AllowedOrigins` em `src\backend\src\SupportAtos.Api\appsettings.Development.json`.

### Porta 5177 ou 5173 em uso

Encerre o processo que esta usando a porta ou altere a URL do backend e o `VITE_PROXY_TARGET` no `.env`.

## 6. Estrutura do projeto

```text
src/
  backend/
    src/
      SupportAtos.Api/
      SupportAtos.Application/
      SupportAtos.Domain/
      SupportAtos.Infrastructure/
    database/
      seed.sql
      seed_mock.sql
      seed_chamados_exemplo.sql
  frontend/
    src/
      api/
      components/
      SupportPortal.jsx
      data.js
      theme.js
database/
docs/
```

## 7. Documentacao

A pasta `docs/` contem a visao geral, requisitos, arquitetura, casos de uso e detalhes de implementacao.

## Aviso final

Este repositorio esta configurado para testes locais com autenticacao desativada. Antes de qualquer uso em producao, reative a autenticacao e revise todos os pontos marcados com `AUTH-DESATIVADA`.
