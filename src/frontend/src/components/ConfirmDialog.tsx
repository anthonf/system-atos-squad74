import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Typography } from '@mui/material';

interface Props {
  aberto: boolean;
  titulo: string;
  descricao: string;
  confirmLabel?: string;
  onFechar: () => void;
  onConfirmar: () => void;
  carregando?: boolean;
}

export function ConfirmDialog({ aberto, titulo, descricao, confirmLabel = 'Confirmar', onFechar, onConfirmar, carregando }: Props) {
  return (
    <Dialog open={aberto} onClose={onFechar} aria-labelledby="confirm-title">
      <DialogTitle id="confirm-title">{titulo}</DialogTitle>
      <DialogContent>
        <Typography>{descricao}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onFechar} disabled={carregando}>
          Cancelar
        </Button>
        <Button onClick={onConfirmar} color="error" variant="contained" disabled={carregando} autoFocus>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
