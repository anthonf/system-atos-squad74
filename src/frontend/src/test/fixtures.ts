import type { Session, Ticket } from '../types';

export const ticketFixture = (overrides: Partial<Ticket> = {}): Ticket => ({
  id: 'AC-2026-00214',
  usuarioId: 1,
  grupoEmpresaId: 1,
  agenteId: 2,
  empresa: 'Banco Confiança',
  contato: 'Fernanda Duarte',
  produto: 'SAP Business One',
  categoria: 'Integração ERP',
  assunto: 'Sincronização de pedidos parada',
  prioridade: 'alta',
  status: 'em_andamento',
  agente: 'Marina Alves',
  slaTotal: 24,
  slaRestante: 3,
  abertura: '2026-09-12T09:14:00.000Z',
  mensagens: [
    {
      autor: 'Fernanda Duarte', autorId: 1, tipo: 'cliente', tipoInteracao: 'Publica',
      data: '2026-09-12T09:14:00.000Z', texto: 'A sincronização de pedidos parou.',
    },
  ],
  notas: [],
  ...overrides,
});

export const AGENTE_SESSION: Session = {
  usuarioId: 2,
  grupoEmpresaId: 1,
  role: 'agente',
  nome: 'Marina Alves',
  empresa: null,
};

export const GESTOR_SESSION: Session = {
  usuarioId: 9,
  grupoEmpresaId: 1,
  role: 'supervisor',
  nome: 'Supervisor Teste',
  empresa: null,
};
