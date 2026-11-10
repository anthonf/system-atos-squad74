// Dados e constantes de domínio. Os dados são mockados; trocar por chamadas de API.

export const STATUS = {
  aberto: { label: 'Aberto', tone: 'info' },
  em_andamento: { label: 'Em andamento', tone: 'warning' },
  aguardando_cliente: { label: 'Aguardando cliente', tone: 'purple' },
  resolvido: { label: 'Resolvido', tone: 'success' },
  fechado: { label: 'Fechado', tone: 'muted' },
};

export const PRIORITY = {
  alta: { label: 'Alta', tone: 'error' },
  media: { label: 'Média', tone: 'warning' },
  baixa: { label: 'Baixa', tone: 'muted' },
};

export const STAGES = ['aberto', 'em_andamento', 'resolvido', 'fechado'];
export const PRODUCTS = ['Joe SFA B1', 'Portal Atos Capital', 'SAP Business One', 'SAP S/4HANA'];
export const CATEGORIES = [
  'Sincronização SSO', 'Integração ERP', 'Erro de sistema',
  'Dúvida de uso', 'Solicitação de acesso', 'Performance',
];
export const EMPRESAS = ['Banco Confiança', 'Cooperativa Vale', 'Financeira Sul', 'Investa Corretora'];
export const AGENTES = ['Marina Alves', 'Diego Ramos'];

/* ---------------------------------------------------------------------- */
/* Mock data                                                               */
/* ---------------------------------------------------------------------- */

export const seed = [
  {
    id: 'AC-2026-00214', empresa: 'Banco Confiança', contato: 'Fernanda Duarte',
    produto: 'SAP Business One', categoria: 'Integração ERP', prioridade: 'alta',
    status: 'em_andamento', agente: 'Marina Alves',
    slaTotal: 24, slaRestante: 3, abertura: '12/09 09:14',
    mensagens: [
      { autor: 'Fernanda Duarte', tipo: 'cliente', data: '12/09 09:14', texto: 'A sincronização de pedidos do Business One parou de refletir no portal desde ontem à noite. Já verificamos a conexão local e está normal.' },
      { autor: 'Marina Alves', tipo: 'agente', data: '12/09 10:02', texto: 'Obrigada pelo detalhamento, Fernanda. Estamos analisando os logs da fila de integração do grupo Banco Confiança agora.' },
      { autor: 'Marina Alves', tipo: 'agente', data: '14/09 08:40', texto: 'Identificamos um travamento no job de sincronização. Aplicando correção e reprocessando os pedidos pendentes.' },
    ],
    notas: [
      { autor: 'Marina Alves', data: '14/09 08:31', texto: 'Job de sync com trava de conexão desde 13/09 23:47. Reiniciar worker e reprocessar fila antes de confirmar ao cliente.' },
    ],
  },
  {
    id: 'AC-2026-00213', empresa: 'Cooperativa Vale', contato: 'Rogério Nunes',
    produto: 'Joe SFA B1', categoria: 'Erro de sistema', prioridade: 'alta',
    status: 'aberto', agente: null,
    slaTotal: 24, slaRestante: 20, abertura: '14/09 07:52',
    mensagens: [
      { autor: 'Rogério Nunes', tipo: 'cliente', data: '14/09 07:52', texto: 'Ao tentar fechar uma visita no Joe SFA B1, o aplicativo apresenta erro e a visita não é salva.' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00212', empresa: 'Banco Confiança', contato: 'Fernanda Duarte',
    produto: 'Portal Atos Capital', categoria: 'Sincronização SSO', prioridade: 'media',
    status: 'aguardando_cliente', agente: 'Diego Ramos',
    slaTotal: 48, slaRestante: 30, abertura: '10/09 14:20',
    aguardandoDesde: '12/09 16:05', prazoAutoEncerramento: '15/09 16:05',
    mensagens: [
      { autor: 'Fernanda Duarte', tipo: 'cliente', data: '10/09 14:20', texto: 'Um novo usuário da filial não está sendo criado automaticamente no portal após o login via SSO.' },
      { autor: 'Diego Ramos', tipo: 'agente', data: '12/09 16:05', texto: 'Poderia confirmar o e-mail corporativo exato usado no login e o grupo empresa esperado (matriz ou filial)? Vamos verificar o registro de sincronização.' },
    ],
    notas: [
      { autor: 'Diego Ramos', data: '12/09 16:06', texto: 'Aguardando confirmação do e-mail para checar upsert na base de usuários. Encerramento automático programado caso não haja resposta.' },
    ],
  },
  {
    id: 'AC-2026-00211', empresa: 'Financeira Sul', contato: 'Paula Menezes',
    produto: 'SAP S/4HANA', categoria: 'Dúvida de uso', prioridade: 'baixa',
    status: 'resolvido', agente: 'Marina Alves',
    slaTotal: 120, slaRestante: 64, abertura: '08/09 11:00',
    mensagens: [
      { autor: 'Paula Menezes', tipo: 'cliente', data: '08/09 11:00', texto: 'Como configuramos o mapeamento de centro de custo entre o S/4HANA e o portal?' },
      { autor: 'Marina Alves', tipo: 'agente', data: '08/09 15:22', texto: 'Enviei o passo a passo por e-mail e deixei um modelo de mapeamento anexado. Ficou claro?' },
      { autor: 'Paula Menezes', tipo: 'cliente', data: '09/09 09:10', texto: 'Perfeito, funcionou. Obrigada!' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00210', empresa: 'Banco Confiança', contato: 'Fernanda Duarte',
    produto: 'Joe SFA B1', categoria: 'Performance', prioridade: 'media',
    status: 'fechado', agente: 'Diego Ramos',
    slaTotal: 48, slaRestante: 0, abertura: '02/09 08:00',
    mensagens: [
      { autor: 'Fernanda Duarte', tipo: 'cliente', data: '02/09 08:00', texto: 'O carregamento da carteira de clientes no app está muito lento nos últimos dias.' },
      { autor: 'Diego Ramos', tipo: 'agente', data: '03/09 10:00', texto: 'Aplicamos um ajuste de índice na base de clientes. Pode validar o carregamento agora?' },
      { autor: 'Fernanda Duarte', tipo: 'cliente', data: '03/09 14:30', texto: 'Melhorou bastante, pode encerrar.' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00209', empresa: 'Cooperativa Vale', contato: 'Rogério Nunes',
    produto: 'SAP Business One', categoria: 'Solicitação de acesso', prioridade: 'baixa',
    status: 'em_andamento', agente: 'Marina Alves',
    slaTotal: 120, slaRestante: 80, abertura: '13/09 13:00',
    mensagens: [
      { autor: 'Rogério Nunes', tipo: 'cliente', data: '13/09 13:00', texto: 'Precisamos liberar o perfil de analista financeiro para dois novos usuários da filial.' },
      { autor: 'Marina Alves', tipo: 'agente', data: '13/09 16:40', texto: 'Poderia confirmar os e-mails corporativos dos dois usuários para liberarmos o perfil?' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00208', empresa: 'Investa Corretora', contato: 'Bianca Lopes',
    produto: 'Portal Atos Capital', categoria: 'Erro de sistema', prioridade: 'alta',
    status: 'aberto', agente: null,
    slaTotal: 24, slaRestante: 2, abertura: '14/09 06:10',
    mensagens: [
      { autor: 'Bianca Lopes', tipo: 'cliente', data: '14/09 06:10', texto: 'O portal está retornando erro 500 ao tentar gerar o relatório de conciliação diária.' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00207', empresa: 'Financeira Sul', contato: 'Paula Menezes',
    produto: 'SAP Business One', categoria: 'Integração ERP', prioridade: 'media',
    status: 'resolvido', agente: 'Diego Ramos',
    slaTotal: 48, slaRestante: 12, abertura: '11/09 09:30',
    mensagens: [
      { autor: 'Paula Menezes', tipo: 'cliente', data: '11/09 09:30', texto: 'Notas fiscais emitidas no Business One não estão chegando ao portal.' },
      { autor: 'Diego Ramos', tipo: 'agente', data: '11/09 17:00', texto: 'Corrigido um mapeamento de campo na integração. As notas de hoje já devem aparecer.' },
    ],
    notas: [],
  },
  {
    id: 'AC-2026-00206', empresa: 'Investa Corretora', contato: 'Bianca Lopes',
    produto: 'Joe SFA B1', categoria: 'Sincronização SSO', prioridade: 'alta',
    status: 'fechado', agente: 'Marina Alves',
    slaTotal: 24, slaRestante: 0, abertura: '05/09 10:00',
    mensagens: [
      { autor: 'Bianca Lopes', tipo: 'cliente', data: '05/09 10:00', texto: 'Um usuário não conseguiu logar via SSO no app mobile.' },
      { autor: 'Marina Alves', tipo: 'agente', data: '05/09 12:15', texto: 'Reprocessamos o vínculo do usuário com o grupo empresa. Já pode tentar novamente.' },
    ],
    notas: [],
  },
];

export const seedNotifications = [
  { id: 1, texto: 'Chamado AC-2026-00214 recebeu uma atualização de Marina Alves.', data: 'há 5 min', lida: false },
  { id: 2, texto: 'Chamado AC-2026-00212 está aguardando retorno do cliente.', data: 'há 2 h', lida: false },
  { id: 3, texto: 'Chamado AC-2026-00208 está próximo de estourar o SLA.', data: 'há 3 h', lida: false },
  { id: 4, texto: 'Chamado AC-2026-00211 foi marcado como resolvido.', data: 'ontem', lida: true },
];

