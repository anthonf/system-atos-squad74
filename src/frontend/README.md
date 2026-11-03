# SupportAtos — Painel do Atendente

É o painel do **usuário interno** (atendente, supervisor, admin): fila de chamados, detalhe com histórico, resposta, nota interna, troca de status, atribuição e dashboard. Ele consome a API de verdade — **não tem mock**.

> Os comandos desta página assumem: backend em `src/backend/`, frontend em `src/frontend/` (pastas irmãs dentro de `src/`).

## O que você precisa

- Node 20+ e npm
- Backend rodando em `http://localhost:5177` + Postgres (passo a passo no `../backend/README.md` — suba o backend **antes** do frontend)

## Primeira vez (5 minutos)

```powershell
# 1. Backend (janela 1) — resumo; o detalhado está no README do backend
Set-Location -LiteralPath "<caminho>\src\backend"
$env:ASPNETCORE_ENVIRONMENT = "Development"
Set-Location -LiteralPath "src\SupportAtos.Api\bin\Debug\net10.0"
dotnet SupportAtos.Api.dll   # Swagger: http://localhost:5177/swagger

# 2. Frontend (janela 2)
Set-Location -LiteralPath "<caminho>\src\frontend"
npm install
npm run dev                  # abre http://localhost:5173
```

Abra `http://localhost:5173/login` e cole um token de dev. Para gerar (em `src/backend/`, janela 3 ou na mesma janela 1 após parar a API — melhor outra):

```powershell
Set-Location -LiteralPath "<caminho>\src\backend"
.\scripts\mint-token.ps1 -Sub portal-agente -Email agente@atos.com -Name "Agente" -GrupoId grupo-1 -GrupoNome "Grupo Norte"
```

O script já copia o token — é só **Ctrl+V** na tela de login. Só entra `Agente`, `Supervisor` ou `Admin`; se tentar com `Cliente`, o painel avisa que é só para a equipe interna.

> `VITE_API_URL` já vem configurado: em dev o Vite faz proxy de `/api` para `http://localhost:5177`. Você **não** precisa de `.env` para rodar local. Só crie um a partir do `.env.example` se for apontar para outra API.

## Todo dia (1 minuto)

```powershell
# Janela 1: backend via DLL (só se http://localhost:5177/swagger não responder)
# Janela 2:
Set-Location -LiteralPath "<caminho>\src\frontend"
npm run dev
```

O JWT fica **só em memória**. Após F5, cole novamente o mesmo token se ainda estiver válido; se expirou (padrão 60 min), gere outro com `mint-token.ps1`.

O login manual agora consulta `GET /api/usuarios/me` antes de anunciar sucesso. Enquanto aguarda, mostra “Validando acesso…” e bloqueia novos envios. Aceita o JWT sozinho ou com o prefixo `Bearer`. Nome, papel e IDs internos vêm da API, sem decodificação local para estabelecer a identidade.

Um 401 de uma requisição da sessão atual ou o fim da validade encerra a sessão e apresenta aviso no login. Respostas antigas de outra sessão não encerram um login novo. A contagem usa a validade e o relógio retornados pela API. Respostas 403, erros de rede e erros de servidor mantêm a sessão; o usuário pode tentar novamente. Não há renovação automática ou persistência do token no disco.

## O que tem em cada tela

| Rota | O que faz |
|---|---|
| `/login` | Cola o JWT, mostra quem você é (nome, grupo, papel) |
| `/` (Fila) | Filtros, assumir chamado, polling a cada 30s, atalhos `/` (buscar) e `R` (atualizar) |
| `/ticket/:codigo` | Timeline (pública x nota interna), templates de resposta, troca de status com confirmação |
| `/dashboard` | Contadores reais + SLA |

Detalhes que importam: ações irreversíveis (ex.: fechar chamado) sempre pedem confirmação; e-mail aparece mascarado (`ag***@...`, LGPD); tem modo claro/escuro.


## Scripts

| Comando | O que faz |
|---|---|
| `npm run dev` | Dev em `:5173` com proxy `/api → :5177` |
| `npm run build` | `tsc -b && vite build` (valida tipos + gera `dist/`) |
| `npm run test` | Vitest (camada api/utils) |
| `npx eslint src` | Lint |

## Estrutura (para se achar no código)

```
src/
  api/        client.ts (fetch central, timeout 15s, traduz ProblemDetails p/ pt-BR), chamados.ts, usuarios.ts, errors.ts
  auth/       AuthContext.tsx (JWT só em memória), RequireAuth.tsx (trava rota sem login)
  types/      chamado.ts, usuario.ts (espelham os DTOs do C#)
  components/ Layout, StatusChip, PriorityChip, SlaBadge, ConfirmDialog, TemplatesBar, feedback
  pages/      Login, Fila, DetalheTicket, Dashboard
  hooks/      usePolling.ts
  utils/      mask.ts (LGPD), sla.ts, atalhos.ts
  tests/      api.test.ts
```



## Notas para o backend (o frontend mexeu no mínimo)

- `Program.cs`: policy CORS `Frontend` + `app.UseCors("Frontend")`.
- `appsettings.json`: `Frontend:Origins = ["http://localhost:5173"]` (dá para sobrescrever com `Frontend__Origins` em deploy).
