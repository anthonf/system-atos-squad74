// Constantes de domínio. Dados agora vêm da API (src/api.js).

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
  urgente: { label: 'Urgente', tone: 'error' },
};

export const STAGES = ['aberto', 'em_andamento', 'resolvido', 'fechado'];
export const PRODUCTS = ['Joe SFA B1', 'Portal Atos Capital', 'SAP Business One', 'SAP S/4HANA'];
export const CATEGORIES = [
  'Sincronização SSO', 'Integração ERP', 'Erro de sistema',
  'Dúvida de uso', 'Solicitação de acesso', 'Performance',
];
