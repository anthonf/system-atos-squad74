// Constantes visuais de domínio. Os chamados vêm da API (ver api.ts).
import type { PriorityKey, StatusKey, ToneKey } from './types';

export const STATUS: Record<StatusKey, { tone: ToneKey }> = {
  aberto: { tone: 'info' },
  em_andamento: { tone: 'warning' },
  aguardando_cliente: { tone: 'purple' },
  resolvido: { tone: 'success' },
  fechado: { tone: 'muted' },
};

export const PRIORITY: Record<PriorityKey, { tone: ToneKey }> = {
  alta: { tone: 'error' },
  media: { tone: 'warning' },
  baixa: { tone: 'muted' },
};

/** Etapas do ciclo de vida mostradas no Stepper (AguardandoCliente não é etapa). */
export const STAGES: StatusKey[] = ['aberto', 'em_andamento', 'resolvido', 'fechado'];
