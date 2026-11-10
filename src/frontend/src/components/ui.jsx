// Peças reutilizáveis: marca, chips de status, SLA, cartões de indicador.
import React from 'react';
import { Box, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { WarningAmberRounded } from '@mui/icons-material';
import { STATUS, PRIORITY } from '../data';
import { MONO } from '../theme';

export function BrandMark({ onDark, width = 150 }) {
  return (
    <Box
      component="img"
      src={onDark ? '/atos-capital-logo-white.png' : '/atos-capital-logo.png'}
      alt="Atos Capital"
      sx={{ width, height: 'auto', display: 'block' }}
    />
  );
}

export function Eyebrow({ children, color = 'primary.main', sx }) {
  return (
    <Typography variant="overline" component="span" sx={{ display: 'block', color, mb: 0.5, ...sx }}>
      {children}
    </Typography>
  );
}

export function StatusChip({ status }) {
  const { atos } = useTheme();
  const s = STATUS[status];
  const tone = atos.tones[s.tone];
  return (
    <Chip
      size="small"
      label={s.label}
      icon={<Box component="span" sx={{ width: 6, height: 6, borderRadius: '50%', bgcolor: tone.fg, ml: '8px !important' }} />}
      sx={{ bgcolor: tone.bg, color: tone.fg, height: 24 }}
    />
  );
}

export function PriorityLabel({ level }) {
  const { atos } = useTheme();
  const p = PRIORITY[level];
  return <Typography variant="body2" sx={{ color: atos.tones[p.tone].fg, fontWeight: 600, fontSize: 12 }}>{p.label}</Typography>;
}

export const isOpenTicket = (t) => !['resolvido', 'fechado'].includes(t.status);
export const isAtRisk = (t) => t.slaRestante / t.slaTotal < 0.2 && isOpenTicket(t);

export function SlaBar({ ticket }) {
  const { atos } = useTheme();
  const pct = Math.max(0, Math.min(100, (ticket.slaRestante / ticket.slaTotal) * 100));
  const risk = isAtRisk(ticket);
  const done = !isOpenTicket(ticket);
  const color = risk ? 'error' : done ? 'inherit' : 'primary';
  const label = ticket.status === 'fechado' ? 'concluído' : ticket.status === 'resolvido' ? 'cumprido' : `${ticket.slaRestante}h restantes`;
  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="baseline" mb={0.75}>
        <Typography variant="caption" sx={{ fontWeight: 500, color: 'text.secondary' }}>Prazo de SLA</Typography>
        <Typography sx={{ fontFamily: MONO, fontSize: 12, fontWeight: 500, color: risk ? 'error.main' : 'text.secondary' }}>{label}</Typography>
      </Stack>
      <LinearProgress variant="determinate" value={pct} color={color} sx={{ height: 6, borderRadius: 3, bgcolor: atos.borderDefault, color: 'text.disabled' }} />
      {risk && (
        <Stack direction="row" alignItems="center" spacing={0.5} mt={1} sx={{ color: 'error.main' }}>
          <WarningAmberRounded sx={{ fontSize: 14 }} />
          <Typography variant="caption" sx={{ color: 'error.main' }}>Próximo de estourar o prazo</Typography>
        </Stack>
      )}
    </Box>
  );
}

export function StatTile({ label, value, detail, tone = 'muted', icon: Icon }) {
  const { atos } = useTheme();
  const c = tone === 'neutral' ? { fg: atos.textPrimary, bg: atos.surfaceAlt } : atos.tones[tone];
  const valueColor = tone === 'muted' || tone === 'neutral' ? 'text.primary' : c.fg;
  return (
    <Paper variant="outlined" sx={{ p: 2.25, minHeight: 112, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
      <Box>
        <Typography variant="caption" sx={{ fontWeight: 500, color: 'text.secondary' }}>{label}</Typography>
        <Typography sx={{ fontSize: 26, fontWeight: 600, color: valueColor, letterSpacing: '-0.6px', my: 0.5 }}>{value}</Typography>
        <Typography variant="caption">{detail}</Typography>
      </Box>
      <Box sx={{ width: 38, height: 38, borderRadius: 2, display: 'grid', placeItems: 'center', bgcolor: tone === 'muted' || tone === 'neutral' ? atos.surfaceAlt : c.bg, color: tone === 'muted' || tone === 'neutral' ? 'text.secondary' : c.fg, flexShrink: 0 }}>
        <Icon sx={{ fontSize: 20 }} />
      </Box>
    </Paper>
  );
}
