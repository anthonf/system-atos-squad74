// Tipos de domínio compartilhados pelo front.

export type ThemeMode = 'light' | 'dark';
export type Role = 'cliente' | 'agente' | 'admin';

export type StatusKey = 'aberto' | 'em_andamento' | 'aguardando_cliente' | 'resolvido' | 'fechado';
export type PriorityKey = 'alta' | 'media' | 'baixa';
export type ToneKey = 'info' | 'warning' | 'error' | 'success' | 'purple' | 'muted';

export interface Message {
  autor: string;
  tipo: 'cliente' | 'agente';
  /** Data/hora em ISO 8601 (exibida com formatDateTime). */
  data: string;
  texto: string;
}

export interface Note {
  autor: string;
  /** Data/hora em ISO 8601. */
  data: string;
  texto: string;
}

export interface Ticket {
  id: string;
  empresa: string;
  contato: string;
  produto: string;
  categoria: string;
  /** Título informado na abertura (obrigatório para chamados criados pela API). */
  assunto?: string;
  prioridade: PriorityKey;
  status: StatusKey;
  agente: string | null;
  slaTotal: number;
  slaRestante: number;
  /** Datas em ISO 8601 (exibidas com formatDateTime, DD/MM/YYYY). */
  abertura: string;
  aguardandoDesde?: string;
  prazoAutoEncerramento?: string;
  mensagens: Message[];
  notas: Note[];
}

export interface Session {
  role: Role;
  nome: string;
  empresa: string | null;
  email?: string;
}

export interface AppNotification {
  id: number;
  texto: string;
  data: string;
  lida: boolean;
}
