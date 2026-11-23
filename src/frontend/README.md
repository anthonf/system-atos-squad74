# Atos Capital — Ferramenta de Suporte (Unidade Bank)

Front-end do MVP do termo de abertura: login, fila de atendimento com SLA, resposta
a chamados, notas internas, notificações e relatórios. Fila, detalhe, interações, status,
atribuição, abertura de chamado e cadastro de usuários usam a **API real** (`src/api.ts`).
`src/data.ts` guarda apenas constantes visuais de status/prioridade/etapas; as fixture de teste ficam em `src/test/fixtures.ts`.

## Como rodar

O [README principal](../../README.md) é a fonte oficial do setup. Na raiz, `setup.ps1` prepara o ambiente interativamente e `start.ps1` inicia os serviços; `-CheckOnly` verifica pré-requisitos sem alterações. Os comandos abaixo são a alternativa manual.
Execute os comandos abaixo na raiz do repositório, após preparar banco, schema, conexão e autenticação. Recomenda-se Node.js 22 LTS e SDK .NET 10 estável; SDK 10 ou superior, incluindo Preview, é aceito após validação de compatibilidade. Os runtimes .NET/ASP.NET Core 10 continuam necessários.
O certificado de desenvolvimento fica no perfil do Windows, fora do Git; nunca copie sua chave para o projeto.

```powershell
dotnet dev-certs https --trust
$certDir = Join-Path $env:USERPROFILE '.aspnet\https'
New-Item -ItemType Directory -Force -Path $certDir | Out-Null
# Exporte uma vez; preserve o par existente nas próximas execuções.
if (!(Test-Path (Join-Path $certDir 'supportatos-frontend.pem')) -and
    !(Test-Path (Join-Path $certDir 'supportatos-frontend.key'))) {
    dotnet dev-certs https --export-path (Join-Path $certDir 'supportatos-frontend.pem') --format Pem --no-password
}
```

Backend, em um terminal (encerre primeiro a instância antiga para recarregar CORS/CSRF):

```powershell
dotnet run --project .\src\backend\src\SupportAtos.Api\SupportAtos.Api.csproj --launch-profile https
```

Frontend, em outro terminal:

```powershell
cd .\src\frontend
npm ci
npm run dev
```

Abra **https://localhost:5173**. A porta é fixa e não há fallback para HTTP. Use o certificado confiável preparado acima.
Se faltar um dos arquivos, restaure o par antes de iniciar. Caminhos alternativos podem ser definidos em
`SUPPORTATOS_HTTPS_CERT` e `SUPPORTATOS_HTTPS_KEY` (veja `.env.example`); mantenha-os fora do repositório.
A API em Development autoriza explicitamente essa origem para CORS e CSRF, preservando as origens anteriores.
Não é necessário alterar banco, usuários ou cookies: continuam HttpOnly, Secure e SameSite=Lax.
O `npm run preview` não substitui este ambiente autenticado; use `npm run dev` para esta validação.

### Telas que usam a API real (chamados, usuários e autenticação)

A autenticação interna usa `POST /api/auth/login`, enviando `{ email, senha }`. A URL da API vem de
`VITE_API_URL` (padrão `https://localhost:7099`; exemplo em `.env.example`). Nome, papel, identificador e grupo
vêm do backend. O JWT permanece somente na memória da aba; não existe fallback para `VITE_API_TOKEN` ou Storage.
O token legado `atos_token` é removido de Storage na inicialização.

`/api/auth/login`, `/refresh` e `/logout` usam `credentials: 'include'`. O navegador administra o cookie
HttpOnly; JavaScript não o lê. Ao carregar a página, o portal aguarda uma única restauração pelo refresh antes
de renderizar login ou telas protegidas. Requisições simultâneas compartilham o refresh; uma resposta 401 pode
ser repetida uma única vez com o novo JWT. Erros 403 não provocam refresh. Falha de renovação encerra o estado local.
O logout limpa a memória imediatamente mesmo com falha de rede, mas a revogação no servidor exige sucesso da chamada:
se ela falhar, um cookie ainda válido poderá restaurar a sessão numa nova carga. A coordenação é por aba.

### Fila e detalhe com dados reais

Todas as requisições de chamado passam por `request()` em `src/api.ts` (JWT em memória, uma renovação e uma repetição após 401):

- `GET /api/chamados` — fila da equipe; clientes recebem `?grupoEmpresaId=` da sessão.
- `GET /api/chamados/{codigo}` — detalhe com interações e nomes reais (autor, empresa e agente).
- `POST /api/chamados/{codigo}/interacoes` — resposta pública (`Publica`) ou nota interna (`NotaInterna`).
- `PATCH /api/chamados/{codigo}/status` e `PATCH /api/chamados/{codigo}/atribuir` — status e atribuição.
- `GET /api/usuarios/agentes` — lista de agentes para repriorização/supervisão (somente equipe).
- `GET /api/sla-categorias` — opções reais de produto/categoria do modal de abertura.
- `POST /api/chamados/interno` — abre chamado usando o usuário autenticado e exige grupo de empresa; por isso o botão "Abrir novo chamado" some quando a sessão não tem grupo.

`chamadoDtoParaTicket()` converte os DTOs para o modelo das telas: converte datas com `parseApiDate`, calcula SLA em horas a partir de `criadoEm`/`slaPrazo`, separa `Publica` em mensagens e `NotaInterna` em notas e preenche `usuarioNome`, `grupoEmpresaNome` e `agenteNome`. O stepper mantém as quatro etapas (`Aberto → Em andamento → Resolvido → Fechado`); `Aguardando cliente` aparece no status e no aviso de prazo, mas não é etapa. Chamado `Fechado` desabilita formulários e ações.

Regras de permissão da API refletidas na interface: agente assume apenas chamados sem agente e não desatribui; supervisor/admin podem assumir, repassar, desatribuir e usar atribuição automática. A transição para "Aguardando cliente" é feita pelo cliente ou pelo fluxo da API; a resposta da equipe em `Aberto` vira `Em andamento` no backend.

### Validação manual da autenticação

1. Use uma conta de teste já ativada e uma API com certificado HTTPS confiável. Não compartilhe tokens ou senhas em logs/screenshots.
2. Defina `VITE_API_URL=https://localhost:7099` no ambiente do Vite e reinicie-o se alterar essa variável.
3. Abra https://localhost:5173 e use a API HTTPS; reinicie a API após mudanças na lista de origens. Faça login novamente para obter o cookie no contexto correto.
4. Entre com senha incorreta: deve continuar no login e mostrar o erro. Entre com credenciais válidas: confira nome e
   menus conforme o papel retornado. O texto do e-mail não concede permissão.
5. Recarregue uma rota interna: aparece carregamento durante `/refresh` e depois a mesma rota, sem novo login.
6. Aguarde o vencimento do acesso ou simule 401 em ambiente de teste e execute ações simultâneas: somente um refresh
   deve recuperar aquela geração de acesso; cada requisição pode repetir no máximo uma vez.
7. Saia: `/logout` deve limpar a sessão local. Recarregue após logout bem-sucedido: deve permanecer no login.
8. Remova o cookie pelo painel do navegador ou deixe-o expirar em ambiente de teste: a restauração deve retornar ao
   login, sem loop. Confira que nenhum JWT é gravado em localStorage/sessionStorage.

Testes automatizados: `npm test`; build: `npm run build`. Os testes de transporte simulam `fetch` e os de interface
usam jsdom; não validam TLS, CORS, CSRF ou envio real de cookies entre origens. Cadastro/convites/ativação estão fora desta fase.

## Padrão de interface: MUI + tokens

O front segue o padrão da Atos Capital: **Material UI (MUI v5) com tokens do design system**.
Antes ele usava Tailwind; foi migrado para alinhar ao padrão.

- `src/tokens.ts` reúne as paletas clara e escura, as fontes, a escala tipográfica e o raio padrão.
- `src/theme.ts` aplica esses valores ao tema MUI com `createTheme`.
- Tokens que não existem na paleta do MUI ficam em `theme.atos` (ex.: `atos.menuGradient`,
  `atos.tableHeaderBg`, `atos.tones.info.bg`). Use `useTheme()` para lê-los.
- Componentes usados: `Button`, `TextField`, `Dialog`, `Tabs`, `Table`/`TablePagination`,
  `Chip`, `Drawer`, `AppBar`, `Popover`, `Stepper`, `Snackbar`, `Skeleton`, `Breadcrumbs`.
- Ícones: `@mui/icons-material`. Gráficos: `recharts` (cores vindas do tema).
- `src/index.css` contém o reset global.

## Padrões do projeto

- **Notificações:** `sonner` — `toast.success` / `toast.error` / `toast.warning` (`<Toaster/>` em `SupportPortal/index.tsx`).
  Não usar `Snackbar` do MUI. Erros de API passam por `notifyApiError` (`src/notify.ts`; 401/403 viram aviso).
- **Datas:** `dayjs` + `@mui/x-date-pickers`, sempre `DD/MM/YYYY` na interface (`src/dates.ts`). Datas trafegam em ISO 8601.
- **Idiomas:** `i18next` + `react-i18next` (pt / en / es) em `src/i18n/locales`. Todo texto de tela usa `t('chave')`;
  o seletor PT/EN/ES fica no menu lateral e no login (escolha salva em `localStorage`).
- **Formulários:** `react-hook-form` + `Yup` (`yupResolver`). Os erros aparecem em `LineErrorForm`, sob o campo.
  As mensagens do Yup são chaves de tradução (`validation.*`), traduzidas ao exibir.
- **Obrigatoriedade:** campo obrigatório leva `required` (asterisco vermelho no rótulo, definido no tema) desde o início;
  a validação não é o único aviso. `mode: 'onTouched'` valida ao sair do campo, não só no envio.

## Acessibilidade (WCAG 2.1 AA) e responsividade

Já aplicado no código:

- **Contraste:** paleta vinho verificada (texto ≥ 4,5:1; bordas/ícones ≥ 3:1) nos temas claro e escuro; textos translúcidos sobre o
  vinho ≥ 5:1; placeholder com contraste real; texto mínimo de 12 px (legendas, papéis, rodapés).
- **Teclado:** foco sempre visível (`:focus-visible`), link "Pular para o conteúdo", foco levado ao conteúdo ao trocar de tela,
  chamados abertos por botão real (Tab/Enter/Espaço) em vez de linha clicável, `aria-expanded` nos filtros.
- **Leitores de tela:** landmarks (`header`, `nav` com nome, `main`), `aria-current` no menu, títulos em ordem (h1 → h2),
  erros de formulário com `role="alert"` e `aria-describedby`, avisos de resultado (`role="status"`), "não lida" nas notificações,
  gráfico com texto alternativo + tabela oculta, abas com `tabpanel`, rótulos traduzidos na paginação e nos filtros.
- **Movimento:** respeita `prefers-reduced-motion`.
- **Mobile:** sem sobreposição no topo do login (320 px), tabs do login ajustadas, `100dvh` (barra do navegador), tabela de chamados
  com colunas essenciais (prioridade vai para baixo do título), modal de novo chamado em tela cheia, gráfico em barras horizontais,
   gaveta e popover limitados à largura da tela, botões de 44 px no topo, formulários em largura total.
- **Estados remotos:** fila e detalhe mostram `ViewSkeleton`/`CircularProgress` durante o carregamento e cartão com "Tentar novamente" em falha de rede/API.

Para conferir à mão (recomendado antes de entregar): 320 / 360 / 768 / 1024 px no DevTools (sem rolagem horizontal fora de tabelas);
percorrer todas as telas só com Tab/Shift+Tab/Enter/Esc; zoom de 200%; Lighthouse (Accessibility) e a extensão axe DevTools em cada tela,
nos dois temas e nos três idiomas.

## Rotas (React Router) e navegação por papel

`BrowserRouter` em `main.tsx`; rotas em `SupportPortal/index.tsx`. Sem sessão aparece o login em qualquer URL; depois do login a URL `/` vai para `/chamados`.

| Rota | Quem acessa | Tela |
|---|---|---|
| `/chamados` | todos | Fila (equipe/admin) ou "Meus chamados" (cliente) |
| `/chamados/:id` | todos (cliente só da própria empresa) | Detalhe do chamado |
| `/relatorios` | todos | Relatórios |
| `/usuarios` | só administrador | Cadastro de usuários |
| qualquer outra | — | redireciona para `/chamados` |

O menu lateral usa `NavLink` (item ativo + `aria-current="page"`). Em produção o servidor precisa devolver `index.html` para
qualquer rota (fallback de SPA); o `vite dev` e o `vite preview` já fazem isso. A autorização real continua sendo da API.

## Estrutura

O projeto é **TypeScript** (`strict`). Para checar os tipos: `npm run typecheck` (o `npm run build` também checa).

```
src/
├─ main.tsx              # bootstrap
├─ SupportPortal/index.tsx     # estado global, ThemeProvider, Toaster, LocalizationProvider e rotas
├─ types.ts              # tipos de domínio (Ticket, Session, Role, ViewId...)
├─ api.ts                # cliente HTTP da API + tipos dos DTOs (chamados, usuários, grupos)
├─ auth.ts               # sessão em memória, login, restauração, refresh coordenado e logout
├─ dates.ts              # dayjs: formatos DD/MM/YYYY e leitura de datas da API
├─ notify.ts             # notifyApiError (sonner)
├─ tokens.ts             # valores do Design System
├─ theme.ts              # tema MUI (claro/escuro), com a extensão de tipo `theme.atos`
├─ data.ts               # constantes visuais de status, prioridade e etapas
├─ i18n/                 # configuração do i18next e locales pt.ts / en.ts / es.ts
└─ components/
   ├─ form/              # LineErrorForm e FormTextField (react-hook-form)
   ├─ Login.tsx          # tela de login única
   ├─ Layout.tsx         # Sidebar (Drawer) e TopBar (AppBar, notificações)
   ├─ LanguageSwitcher.tsx
   ├─ TicketList/index.tsx     # fila / "Meus chamados", filtros (inclusive por data), modal de novo chamado
   ├─ TicketDetail/index.tsx   # conversa, notas internas, SLA, assumir / repassar
   ├─ Reports.tsx        # relatórios
   ├─ UserAdmin/index.tsx      # administração: cadastro de usuários (API real)
   └─ ui.tsx             # marca, chips de status, SLA, cartões de indicador
```

A logo está em `public/` (`atos-capital-logo.png` e a versão branca para fundos escuros).
Se receber um arquivo novo da marca, remova o fundo branco e gere a versão branca antes de trocar.

## Login único e papéis

Há **uma única tela de login** (e-mail corporativo + senha); o perfil vem da resposta autenticada do backend:

- **Administrador:** vê tudo — Fila de atendimento, Relatórios, **Cadastro de usuários** (`POST /api/usuarios`, lista em
  `GET /api/usuarios?grupoEmpresaId=`, grupos em `GET /api/grupos-empresa`) e também abre chamados.
- **Membro da equipe (Agente/Supervisor):** Fila, detalhe e Relatórios. Supervisor é preservado como papel distinto, sem ganhar o menu exclusivo de Admin.

A autorização real continua sendo feita pela API; esconder uma tela no frontend não substitui RBAC no servidor.

O código ainda tem os ramos da visão de cliente (`role === 'cliente'`), mas nenhum login leva a eles; podem ser removidos quando a equipe confirmar.

## Título obrigatório na abertura de chamado

- **Front** (`NewTicketModal`): Título é obrigatório (asterisco vermelho, erro sob o campo via `LineErrorForm`, até 200 caracteres).
- **API** (`POST /api/chamados/interno`): sem título (ausente, nulo, vazio ou só espaços) responde **400** com `{ detail, errors, campo: "titulo" }`;
  o domínio (`Chamado.Criar`) também recusa título vazio e **não troca mais pela categoria**. `CriarChamadoInput.Titulo` não é mais opcional.
  O front mostra a mensagem da API **sob o campo Título** (erros `type: "server"` aparecem como vieram, sem tradução) e não fecha o formulário.
- **Testes:** front — `npm test` (`src/components/NewTicketModal.test.tsx`, `src/auth.test.ts`); backend — `dotnet test`
  (`CriarChamadoUseCaseTests` e `SupportAtosApiTests.CriarChamado_SemTitulo_DeveRetornar400IndicandoOCampoTitulo`).

## O que ainda é só interface

- "Esqueci minha senha" e "Exportar" são visuais. "Atualizar" recarrega a fila/detalhe da API. "Lembrar acesso" fica marcado e desabilitado:
  a duração do acesso persistente é definida pelo cookie do backend, sem armazenamento de senha no navegador.
- Seleção de colunas persistida (`<ColumnSelector/>` do template de tela) não foi feita.
- Notificações e leitura vivem no estado local; numa integração real viriam do webhook
  de eventos (seção 2.3 do termo).

### Organização das quatro telas (issue #38)

SupportPortal, TicketList, TicketDetail e UserAdmin possuem uma pasta com `index.tsx` para renderização e `useApp.ts` para estado, regras e ações. Os hooks de formulários e resumos ficam junto à tela correspondente. Os imports públicos continuam apontando para a pasta da tela.

O fechamento de 24/11 preserva as interações existentes. Os filtros continuam em Collapse, o detalhe mantém a URL direta `/chamados/:id` e o cadastro continua na página de usuários. A migração desses fluxos para modais exige avaliação com a equipe conforme a issue #38.
