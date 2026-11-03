import { Box, Typography } from '@mui/material';

export function PageHeader({ titulo, subtitulo }: { titulo: string; subtitulo?: string }) {
  return (
    <Box mb={2}>
      <Typography variant="h5" component="h1" fontWeight={700}>
        {titulo}
      </Typography>
      {subtitulo && (
        <Typography variant="body2" color="text.secondary">
          {subtitulo}
        </Typography>
      )}
    </Box>
  );
}

export function EmptyState({ texto }: { texto: string }) {
  return (
    <Box py={4} textAlign="center" role="status">
      <Typography color="text.secondary">{texto}</Typography>
    </Box>
  );
}

export function ErrorState({ texto, onRetry }: { texto: string; onRetry?: () => void }) {
  return (
    <Box py={4} textAlign="center" role="alert">
      <Typography color="error">{texto}</Typography>
      {onRetry && (
        <button onClick={onRetry} style={{ marginTop: 8 }}>
          Tentar novamente
        </button>
      )}
    </Box>
  );
}
