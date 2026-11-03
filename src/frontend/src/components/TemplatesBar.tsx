import { Box, Button } from '@mui/material';

export const TEMPLATES = [
  { titulo: 'Confirmação de recebimento', texto: 'Olá! Recebemos seu chamado e já estamos analisando. Retornamos em breve.' },
  { titulo: 'Pedido de informação', texto: 'Olá! Para avançar, precisamos que você informe: [descreva o dado]. Assim que responder, retomamos o atendimento.' },
  { titulo: 'Resolução', texto: 'Olá! O problema foi corrigido. Por favor valide e nos avise se precisar de mais alguma coisa.' },
];

export function TemplatesBar({ onUsar }: { onUsar: (texto: string) => void }) {
  return (
    <Box display="flex" gap={1} flexWrap="wrap" mb={1} aria-label="Respostas rápidas">
      {TEMPLATES.map((t) => (
        <Button key={t.titulo} size="small" variant="outlined" onClick={() => onUsar(t.texto)}>
          {t.titulo}
        </Button>
      ))}
    </Box>
  );
}
