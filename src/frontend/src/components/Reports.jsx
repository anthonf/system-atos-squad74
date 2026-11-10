// Relatórios: totais, chamados por produto, categorias recorrentes e chamados próximos do SLA.
import React, { useMemo, useState } from 'react';
import { Box, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableRow, TextField, Typography } from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { AccessTime, CheckCircleOutline, ConfirmationNumberOutlined } from '@mui/icons-material';
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { CATEGORIES, EMPRESAS, PRODUCTS } from '../data';
import { Eyebrow, isAtRisk, StatTile } from './ui';
import { FONT, MONO } from '../theme';

export default function Reports({ tickets, role, user }) {
  const { atos } = useTheme();
  const [empresa, setEmpresa] = useState(role === 'cliente' ? user.empresa : 'todas');
  const scoped = useMemo(() => tickets.filter((t) => empresa === 'todas' || t.empresa === empresa), [tickets, empresa]);

  const porProduto = PRODUCTS.map((p) => ({ nome: p, total: scoped.filter((t) => t.produto === p).length }));
  const porCategoria = CATEGORIES.map((c) => ({ nome: c, total: scoped.filter((t) => t.categoria === c).length })).sort((a, b) => b.total - a.total);
  const resolvidos = scoped.filter((t) => ['resolvido', 'fechado'].includes(t.status)).length;
  const emRisco = scoped.filter(isAtRisk);
  const tick = { fontSize: 11, fontFamily: FONT, fill: atos.textSecondary };

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Eyebrow>Inteligência operacional</Eyebrow>
      <Typography variant="h1" sx={{ fontSize: 30 }}>Relatórios</Typography>
      <Typography color="text.secondary" mt={1} mb={3}>Panorama de chamados por produto, categoria e proximidade do SLA.</Typography>

      {role === 'agente' && (
        <TextField select size="small" label="Grupo empresa" value={empresa} onChange={(e) => setEmpresa(e.target.value)} sx={{ minWidth: 240, mb: 2.5 }}>
          <MenuItem value="todas">Todos os grupos empresa</MenuItem>
          {EMPRESAS.map((e) => <MenuItem key={e} value={e}>{e}</MenuItem>)}
        </TextField>
      )}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 1.5, mb: 4 }}>
        <StatTile label="Chamados no período" value={scoped.length} detail="no filtro atual" tone="neutral" icon={ConfirmationNumberOutlined} />
        <StatTile label="Resolvidos ou fechados" value={resolvidos} detail="concluídos" tone="success" icon={CheckCircleOutline} />
        <StatTile label="Próximos do SLA" value={emRisco.length} detail="exigem atenção" tone={emRisco.length ? 'error' : 'neutral'} icon={AccessTime} />
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 4, mb: 4 }}>
        <Box>
          <Typography variant="h3" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>Chamados por produto</Typography>
          <Box sx={{ height: 230 }}>
            <ResponsiveContainer>
              <BarChart data={porProduto} margin={{ left: -20 }}>
                <CartesianGrid stroke={atos.borderSubtle} vertical={false} />
                <XAxis dataKey="nome" tick={{ ...tick, fontSize: 10 }} interval={0} angle={-15} textAnchor="end" height={50} />
                <YAxis tick={tick} allowDecimals={false} />
                <Tooltip cursor={{ fill: atos.brandSoft }} contentStyle={{ fontFamily: FONT, fontSize: 12, border: `1px solid ${atos.borderDefault}`, background: atos.surfaceBase, color: atos.textPrimary }} />
                <Bar dataKey="total" fill={atos.brandText} radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Box>
        </Box>
        <Box>
          <Typography variant="h3" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>Chamados recorrentes por categoria</Typography>
          <Paper variant="outlined">
            <Table size="small"><TableBody>
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

      <Typography variant="h3" sx={{ color: 'primary.main', textTransform: 'uppercase', letterSpacing: '0.06em', fontSize: 12, mb: 1.5 }}>Chamados próximos do SLA</Typography>
      {emRisco.length === 0 ? (
        <Stack direction="row" spacing={1} alignItems="center" sx={{ py: 1.5, color: 'text.secondary' }}>
          <CheckCircleOutline sx={{ color: 'success.main', fontSize: 18 }} /><Typography>Nenhum chamado em risco no momento.</Typography>
        </Stack>
      ) : (
        <Paper variant="outlined">
          <Table size="small"><TableBody>
            {emRisco.map((t, i) => (
              <TableRow key={t.id} sx={{ bgcolor: i % 2 === 1 ? atos.tableRowAlt : 'background.paper' }}>
                <TableCell sx={{ fontFamily: MONO, fontSize: 12, color: 'text.disabled' }}>{t.id}</TableCell>
                <TableCell>{t.categoria} — {t.empresa}</TableCell>
                <TableCell align="right" sx={{ fontFamily: MONO, fontSize: 12, color: 'error.main' }}>{t.slaRestante}h restantes</TableCell>
              </TableRow>
            ))}
          </TableBody></Table>
        </Paper>
      )}
    </Box>
  );
}
