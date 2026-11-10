// Relatórios: totais, chamados por produto, categorias recorrentes e chamados próximos do SLA.
import React, { useMemo, useState } from 'react';
import { Box, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableRow, TextField, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useTranslation } from 'react-i18next';
import { AccessTime, CheckCircleOutline, ConfirmationNumberOutlined } from '@mui/icons-material';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { CATEGORIES, EMPRESAS, PRODUCTS } from '../data';
import { Eyebrow, isAtRisk, StatTile, visuallyHidden } from './ui';
import { FONT, MONO } from '../theme';
import type { Role, Session, Ticket } from '../types';

interface ReportsProps {
  tickets: Ticket[];
  role: Role;
  user: Session;
}

export default function Reports({ tickets, role, user }: ReportsProps) {
  const { atos } = useTheme();
  const { t: tr } = useTranslation();
  const [empresa, setEmpresa] = useState(role === 'cliente' ? user.empresa : 'todas');
  const scoped = useMemo(() => tickets.filter((t) => empresa === 'todas' || t.empresa === empresa), [tickets, empresa]);

  const porProduto = PRODUCTS.map((p) => ({ nome: p, total: scoped.filter((t) => t.produto === p).length }));
  const porCategoria = CATEGORIES.map((c) => ({ nome: c, total: scoped.filter((t) => t.categoria === c).length })).sort((a, b) => b.total - a.total);
  const resolvidos = scoped.filter((t) => ['resolvido', 'fechado'].includes(t.status)).length;
  const emRisco = scoped.filter(isAtRisk);
  const tick = { fontSize: 12, fontFamily: FONT, fill: atos.textSecondary };
  const theme = useTheme();
  const compact = useMediaQuery(theme.breakpoints.down('sm')); // celular: barras horizontais, sem rótulo inclinado
  const chartSummary = porProduto.map((p) => `${p.nome}: ${p.total}`).join('; ');

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Eyebrow>{tr('reports.eyebrow')}</Eyebrow>
      <Typography variant="h1" sx={{ fontSize: 30 }}>{tr('reports.title')}</Typography>
      <Typography color="text.secondary" mt={1} mb={3}>{tr('reports.subtitle')}</Typography>

      {role !== 'cliente' && (
        <TextField select size="small" label={tr('reports.group')} value={empresa} onChange={(e) => setEmpresa(e.target.value)} sx={{ minWidth: { sm: 240 }, width: { xs: '100%', sm: 'auto' }, mb: 2.5 }}>
          <MenuItem value="todas">{tr('reports.allGroups')}</MenuItem>
          {EMPRESAS.map((e) => <MenuItem key={e} value={e}>{e}</MenuItem>)}
        </TextField>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 1.5, mb: 4 }}>
        <StatTile label={tr('reports.totalPeriod')} value={scoped.length} detail={tr('reports.currentFilter')} tone="neutral" icon={ConfirmationNumberOutlined} />
        <StatTile label={tr('reports.resolved')} value={resolvidos} detail={tr('reports.done')} tone="success" icon={CheckCircleOutline} />
        <StatTile label={tr('reports.nearSla')} value={emRisco.length} detail={tr('reports.needAttention')} tone={emRisco.length ? 'error' : 'neutral'} icon={AccessTime} />
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 4, mb: 4 }}>
        <Box>
          <Typography variant="h3" component="h2" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>{tr('reports.byProduct')}</Typography>
          {/* Gráfico: texto alternativo + tabela só para leitores de tela (o SVG sozinho não é acessível). */}
          <Box role="img" aria-label={tr('reports.chartAlt', { data: chartSummary })} sx={{ height: compact ? porProduto.length * 44 + 30 : 230 }}>
            <ResponsiveContainer>
              <BarChart data={porProduto} layout={compact ? 'vertical' : 'horizontal'} margin={compact ? { left: 0, right: 12 } : { left: -20 }}>
                <CartesianGrid stroke={atos.borderSubtle} vertical={compact} horizontal={!compact} />
                {compact ? <XAxis type="number" tick={tick} allowDecimals={false} /> : <XAxis dataKey="nome" tick={{ ...tick, fontSize: 11 }} interval={0} angle={-15} textAnchor="end" height={50} />}
                {compact ? <YAxis type="category" dataKey="nome" tick={tick} width={118} /> : <YAxis tick={tick} allowDecimals={false} />}
                <Tooltip cursor={{ fill: atos.brandSoft }} contentStyle={{ fontFamily: FONT, fontSize: 12, border: `1px solid ${atos.borderDefault}`, background: atos.surfaceBase, color: atos.textPrimary }} />
                <Bar dataKey="total" fill={atos.brandText} radius={compact ? [0, 3, 3, 0] : [3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Box>
          <Box component="table" sx={visuallyHidden}>
            <caption>{tr('reports.byProduct')}</caption>
            <tbody>{porProduto.map((p) => <tr key={p.nome}><th scope="row">{p.nome}</th><td>{p.total}</td></tr>)}</tbody>
          </Box>
        </Box>
        <Box>
          <Typography variant="h3" component="h2" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>{tr('reports.byCategory')}</Typography>
          <Paper variant="outlined">
            <Table size="small" aria-label={tr('reports.byCategory')}><TableBody>
              {porCategoria.map((c, i) => (
                <TableRow key={c.nome} sx={{ bgcolor: i % 2 === 1 ? atos.tableRowAlt : 'background.paper' }}>
                  <TableCell>{c.nome}</TableCell>
                  <TableCell align="right" sx={{ fontFamily: MONO, color: 'text.secondary' }}>{c.total}</TableCell>
                </TableRow>
              ))}
            </TableBody></Table>
          </Paper>
        </Box>
      </Box>

      <Typography variant="h3" component="h2" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>{tr('reports.atRiskTitle')}</Typography>
      {emRisco.length === 0 ? (
        <Stack direction="row" spacing={1} alignItems="center" sx={{ py: 1.5, color: 'text.secondary' }}>
          <CheckCircleOutline sx={{ color: 'success.main', fontSize: 18 }} /><Typography>{tr('reports.noRisk')}</Typography>
        </Stack>
      ) : (
        <Paper variant="outlined">
          <Table size="small" aria-label={tr('reports.atRiskTitle')}><TableBody>
            {emRisco.map((t, i) => (
              <TableRow key={t.id} sx={{ bgcolor: i % 2 === 1 ? atos.tableRowAlt : 'background.paper' }}>
                <TableCell sx={{ fontFamily: MONO, fontSize: 12, color: 'text.secondary' }}>{t.id}</TableCell>
                <TableCell>{t.categoria} — {t.empresa}</TableCell>
                <TableCell align="right" sx={{ fontFamily: MONO, fontSize: 12, color: 'error.main' }}>{tr('sla.remaining', { hours: t.slaRestante })}</TableCell>
              </TableRow>
            ))}
          </TableBody></Table>
        </Paper>
      )}
    </Box>
  );
}
