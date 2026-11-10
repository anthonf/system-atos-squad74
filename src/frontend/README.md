# Atos Capital — Ferramenta de Suporte (Unidade Bank)

Front-end do MVP do termo de abertura: login, fila de atendimento com SLA, resposta
a chamados, notas internas, notificações e relatórios. A **abertura de chamado já usa a API real**
(`src/api.js`); o restante das telas ainda usa dados mockados (`src/data.js`).

## Como rodar

```bash
npm install
npm run dev     # http://localhost:5173
```

Requer Node.js 18 ou superior.

### Telas que usam a API real (abertura de chamado e cadastro de usuários)

Em desenvolvimento (`npm run dev`) o login do portal pede um token de dev à API (`POST /api/dev/token`) e o guarda em
`sessionStorage` ("atos_token"); é só subir a API em Development (veja `backend/README.md`). A URL da API vem de
`VITE_API_URL` (padrão `http://localhost:5177`). Alternativa manual: gere um token com `backend/scripts/mint-token.ps1` e
coloque em `VITE_API_TOKEN` no `.env.local` (copie de `.env.example`). Em produção o SSO deve gravar o token em
`sessionStorage` na chave `atos_token`.

## Padrão de interface: MUI + tokens

O front segue o padrão da Atos Capital: **Material UI (MUI v5) com tokens do design system**.
Antes ele usava Tailwind; foi migrado para alinhar ao padrão.

- `src/theme.js` é a **fonte única dos tokens** (marca, superfícies, texto, estados, menu,
  tabela, scrollbar), em claro e escuro, e monta o tema com `createTheme`. Também define a
  escala tipográfica do doc (título 24/600, seção 16/600, corpo 13-14, rótulo 12/500, apoio 11).
- Tokens que não existem na paleta do MUI ficam em `theme.atos` (ex.: `atos.menuGradient`,
  `atos.tableHeaderBg`, `atos.tones.info.bg`). Use `useTheme()` para lê-los.
- Componentes usados: `Button`, `TextField`, `Dialog`, `Tabs`, `Table`/`TablePagination`,
  `Chip`, `Drawer`, `AppBar`, `Popover`, `Stepper`, `Snackbar`, `Skeleton`, `Breadcrumbs`.
- Ícones: `@mui/icons-material`. Gráficos: `recharts` (cores vindas do tema).
- `src/index.css` só tem o reset mínimo. Para mudar cor, fonte ou espaçamento global, edite
  `theme.js`, não os componentes.

## Padrões do projeto

- **Notificações:** `sonner` — `toast.success` / `toast.error` / `toast.warning` (`<Toaster/>` em `SupportPortal.tsx`).
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

Para conferir à mão (recomendado antes de entregar): 320 / 360 / 768 / 1024 px no DevTools (sem rolagem horizontal fora de tabelas);
percorrer todas as telas só com Tab/Shift+Tab/Enter/Esc; zoom de 200%; Lighthouse (Accessibility) e a extensão axe DevTools em cada tela,
nos dois temas e nos três idiomas.

## Rotas (React Router) e navegação por papel

`BrowserRouter` em `main.tsx`; rotas em `SupportPortal.tsx`. Sem sessão aparece o login em qualquer URL; depois do login a URL `/` vai para `/chamados`.

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
├─ SupportPortal.tsx     # estado global, ThemeProvider, Toaster, LocalizationProvider e rotas
├─ types.ts              # tipos de domínio (Ticket, Session, Role, ViewId...)
├─ api.ts                # cliente HTTP da API + tipos dos DTOs (chamados, usuários, grupos)
├─ dates.ts              # dayjs: formatos DD/MM/YYYY e leitura de datas da API
├─ notify.ts             # notifyApiError (sonner)
├─ theme.ts              # tokens + tema MUI (claro/escuro), com a extensão de tipo `theme.atos`
├─ data.ts               # constantes de domínio e dados mockados da fila
├─ i18n/                 # configuração do i18next e locales pt.ts / en.ts / es.ts
└─ components/
   ├─ form/              # LineErrorForm e FormTextField (react-hook-form)
   ├─ Login.tsx          # vitrine do produto + cartão de acesso (Cliente / Equipe / Administrador)
   ├─ Layout.tsx         # Sidebar (Drawer) e TopBar (AppBar, notificações)
   ├─ LanguageSwitcher.tsx
   ├─ TicketList.tsx     # fila / "Meus chamados", filtros (inclusive por data), modal de novo chamado
   ├─ TicketDetail.tsx   # conversa, notas internas, SLA, assumir / repassar
   ├─ Reports.tsx        # relatórios
   ├─ UserAdmin.tsx      # administração: cadastro de usuários (API real)
   └─ ui.tsx             # marca, chips de status, SLA, cartões de indicador
```

A logo está em `public/` (`atos-capital-logo.png` e a versão branca para fundos escuros).
Se receber um arquivo novo da marca, remova o fundo branco e gere a versão branca antes de trocar.

## Papéis

- **Administrador** (aba "Administrador" no login): vê **Fila de atendimento**, **Relatórios** e **Cadastro de usuários**
  (`POST /api/usuarios`, lista em `GET /api/usuarios?grupoEmpresaId=` e grupos em `GET /api/grupos-empresa`), para
  cadastrar clientes e demais perfis. O perfil real é validado pela API a partir do token (papel `Admin`); a tela não
  substitui a autorização do servidor.
- **Cliente** (login "Atos Capital ID", SSO): vê só os chamados da própria empresa, abre
  novos chamados e acompanha o andamento.
- **Agente** (e-mail e senha): vê toda a fila, assume e repassa chamados, usa notas internas.
  Não abre chamado.

## O que ainda é só interface

- "Manter conectado", "Esqueci minha senha", "Exportar" e "Atualizar" são visuais.
- Seleção de colunas persistida (`<ColumnSelector/>` do template de tela) não foi feita.
- Notificações e leitura vivem no estado local; numa integração real viriam do webhook
  de eventos (seção 2.3 do termo).
- Autenticação é simulada (o SSO/JWT real entra na seção 6 do termo).
