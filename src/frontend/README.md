# Atos Capital — Ferramenta de Suporte (Unidade Bank)

Front-end do MVP do termo de abertura: login, fila de atendimento com SLA, resposta
a chamados, notas internas, notificações e relatórios. Os dados são mockados
(`src/data.js`); ainda não há integração com API.

## Como rodar

```bash
npm install
npm run dev     # http://localhost:5173
```

Requer Node.js 18 ou superior.

## Padrão de interface: MUI + tokens

O front segue o padrão da Atos Bank: **Material UI (MUI v5) com tokens do design system**.
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

## Estrutura

```
src/
├─ main.jsx              # bootstrap
├─ SupportPortal.jsx     # estado global, ThemeProvider e troca de telas
├─ theme.js              # tokens + tema MUI (claro/escuro)
├─ data.js               # constantes de domínio e dados mockados
└─ components/
   ├─ Login.jsx          # vitrine do produto + cartão de acesso
   ├─ Layout.jsx         # Sidebar (Drawer) e TopBar (AppBar, notificações)
   ├─ TicketList.jsx     # fila / "Meus chamados", resumo do cliente, modal de novo chamado
   ├─ TicketDetail.jsx   # conversa, notas internas, SLA, assumir / repassar
   ├─ Reports.jsx        # relatórios
   └─ ui.jsx             # marca, chips de status, SLA, cartões de indicador
```

A logo está em `public/` (`atos-capital-logo.png` e a versão branca para fundos escuros).
Se receber um arquivo novo da marca, remova o fundo branco e gere a versão branca antes de trocar.

## Papéis

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
