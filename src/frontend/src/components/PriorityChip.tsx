import { Chip } from '@mui/material';
import { PRIORIDADE_LABEL, type Prioridade } from '../types/chamado';

export function PriorityChip({ prioridade }: { prioridade: Prioridade }) {
  const color = prioridade === 'Urgente' ? 'error' : prioridade === 'Alta' ? 'warning' : 'default';
  return <Chip label={PRIORIDADE_LABEL[prioridade]} color={color} size="small" variant={prioridade === 'Baixa' ? 'outlined' : 'filled'} />;
}
