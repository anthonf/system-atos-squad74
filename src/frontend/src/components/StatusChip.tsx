import { Chip, type ChipProps } from '@mui/material';
import { STATUS_LABEL, type StatusChamado } from '../types/chamado';

const COR: Record<StatusChamado, ChipProps['color']> = {
  Aberto: 'error',
  EmAndamento: 'warning',
  AguardandoCliente: 'info',
  Resolvido: 'success',
  Fechado: 'default',
};

export function StatusChip({ status }: { status: StatusChamado }) {
  return <Chip label={STATUS_LABEL[status]} color={COR[status]} size="small" aria-label={`Status ${STATUS_LABEL[status]}`} />;
}
