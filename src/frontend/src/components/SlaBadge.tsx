import { Chip, Tooltip } from '@mui/material';
import { slaEstourado, tempoRestante } from '../utils/sla';

export function SlaBadge({ slaPrazo, status }: { slaPrazo: string; status: string }) {
  const estourado = slaEstourado(slaPrazo, status);
  return (
    <Tooltip title={new Date(slaPrazo).toLocaleString('pt-BR')}>
      <Chip
        label={tempoRestante(slaPrazo)}
        color={estourado ? 'error' : 'default'}
        size="small"
        aria-label={estourado ? 'SLA estourado' : 'Prazo SLA'}
      />
    </Tooltip>
  );
}
