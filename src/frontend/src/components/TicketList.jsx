// Fila / "Meus chamados": indicadores, busca, filtros, tabela paginada, resumo do cliente e modal de novo chamado.
import React, { useEffect, useMemo, useState } from 'react';
import {
  Box, Button, Chip, CircularProgress, Collapse, Dialog, DialogActions, DialogContent, DialogTitle, IconButton,
  InputAdornment, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination,
  TableRow, TextField, Tooltip, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import {
  Add, AccessTime, BarChartOutlined, CheckCircleOutline, ChevronRight, Close, ConfirmationNumberOutlined,
  FileDownloadOutlined, HeadsetMicOutlined, InboxOutlined, Refresh, Search, Tune,
} from '@mui/icons-material';
import { CATEGORIES, PRODUCTS, STATUS } from '../data';
import { Eyebrow, isAtRisk, isOpenTicket, PriorityLabel, StatTile, StatusChip } from './ui';
import { MONO } from '../theme';

function CustomerInsights({ tickets }) {
  const { atos } = useTheme();
  const doing = tickets.filter((t) => ['aberto', 'em_andamento'].includes(t.status)).length;
  const waiting = tickets.filter((t) => t.status === 'aguardando_cliente').length;
  const done = tickets.filter((t) => ['resolvido', 'fechado'].includes(t.status)).length;
  const groups = [
    { label: 'Em atendimento', value: doing, color: atos.warning },
    { label: 'Aguardando você', value: waiting, color: atos.purple },
    { label: 'Concluídos', value: done, color: atos.textMuted },
  ];
  const total = Math.max(1, doing + waiting + done);
  const updates = tickets.slice(0, 2).map((t) => {
    const last = t.mensagens[t.mensagens.length - 1];
    return {
      id: t.id,
      titulo: last ? `${last.autor.split(' ')[0]} ${last.tipo === 'agente' ? 'respondeu' : 'escreveu'}` : 'Chamado registrado',
      quando: last ? last.data : t.abertura,
      agente: Boolean(last && last.tipo === 'agente'),
    };
  });

  return (
    <Box component="section" aria-label="Resumo dos chamados" sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '2fr 3fr' }, gap: 2, mt: 3 }}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box><Eyebrow>Visão geral</Eyebrow><Typography variant="h2">Chamados por status</Typography></Box>
          <Box sx={{ width: 36, height: 36, display: 'grid', placeItems: 'center', borderRadius: 2, bgcolor: atos.brandSoft, color: 'primary.main' }}><BarChartOutlined fontSize="small" /></Box>
        </Stack>
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 1.5, mt: 2.5 }}>
          {groups.map((g) => (
            <Box key={g.label}>
              <Stack direction="row" alignItems="center" spacing={0.75}>
                <Box sx={{ width: 6, height: 6, borderRadius: '50%', bgcolor: g.color }} />
                <Typography variant="caption">{g.label}</Typography>
              </Stack>
              <Typography sx={{ fontSize: 18, fontWeight: 600 }}>{g.value}</Typography>
            </Box>
          ))}
        </Box>
        <Stack direction="row" spacing={0.5} sx={{ mt: 2, height: 6, borderRadius: 3, overflow: 'hidden' }} aria-hidden>
          {groups.map((g) => (g.value > 0 ? <Box key={g.label} sx={{ flex: g.value / total, bgcolor: g.color }} /> : null))}
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Eyebrow>Acompanhamento</Eyebrow>
        <Typography variant="h2">Atualizações recentes</Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 1.75, mt: 2 }}>
          {updates.length === 0 && <Typography color="text.disabled">Nenhuma atualização ainda.</Typography>}
          {updates.map((u) => (
            <Stack key={u.id} direction="row" spacing={1.25} alignItems="center" sx={{ p: 1.5, borderRadius: 2, bgcolor: atos.surfaceAlt }}>
              <Box sx={{ width: 30, height: 30, display: 'grid', placeItems: 'center', borderRadius: '50%', flexShrink: 0, bgcolor: u.agente ? atos.warningBg : atos.successBg, color: u.agente ? atos.warning : atos.success }}>
                {u.agente ? <AccessTime sx={{ fontSize: 16 }} /> : <CheckCircleOutline sx={{ fontSize: 16 }} />}
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography noWrap sx={{ fontSize: 12, fontWeight: 600 }}>{u.titulo}</Typography>
                <Typography variant="caption">{u.id} · {u.quando}</Typography>
              </Box>
            </Stack>
          ))}
        </Box>
      </Paper>
    </Box>
  );
}

export function NewTicketModal({ user, onClose, onCreate }) {
  const [categoria, setCategoria] = useState('');
  const [produto, setProduto] = useState(PRODUCTS[0]);
  const [assunto, setAssunto] = useState('');
  const [descricao, setDescricao] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const submit = (e) => {
    e.preventDefault();
    if (!categoria || !assunto.trim() || !descricao.trim()) {
      setError('Preencha categoria, assunto e descrição.');
      return;
    }
    setError('');
    setSubmitting(true);
    setTimeout(() => {
      const id = `AC-2026-${String(Math.floor(215 + Math.random() * 700)).padStart(5, '0')}`;
      onCreate({
        id, empresa: user.empresa, contato: user.nome, produto, categoria, assunto: assunto.trim(),
        prioridade: 'media', status: 'aberto', agente: null, slaTotal: 48, slaRestante: 48, abertura: 'agora',
        mensagens: [{ autor: user.nome, tipo: 'cliente', data: 'agora', texto: descricao.trim() }],
        notas: [],
      });
    }, 500);
  };

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="xs" PaperProps={{ component: 'form', onSubmit: submit, noValidate: true }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box><Eyebrow>Novo atendimento</Eyebrow><Typography variant="h2" sx={{ fontSize: 18 }}>Abrir chamado</Typography></Box>
          <IconButton aria-label="Fechar" onClick={onClose} size="small"><Close /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField select label="Categoria" value={categoria} onChange={(e) => setCategoria(e.target.value)} fullWidth size="small" autoFocus>
            {CATEGORIES.map((c) => <MenuItem key={c} value={c}>{c}</MenuItem>)}
          </TextField>
          <TextField select label="Produto ou módulo" value={produto} onChange={(e) => setProduto(e.target.value)} fullWidth size="small">
            {PRODUCTS.map((p) => <MenuItem key={p} value={p}>{p}</MenuItem>)}
          </TextField>
          <TextField label="Assunto" value={assunto} onChange={(e) => setAssunto(e.target.value)} placeholder="Resuma o que está acontecendo" fullWidth size="small" />
          <TextField label="Descrição" value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Descreva o problema com o máximo de detalhes" multiline minRows={4} fullWidth size="small" />
          {error && <Typography variant="caption" sx={{ color: 'error.main' }}>{error}</Typography>}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button variant="outlined" onClick={onClose}>Cancelar</Button>
        <Button type="submit" variant="contained" disabled={submitting} startIcon={submitting ? <CircularProgress size={14} color="inherit" /> : null} endIcon={!submitting && <ChevronRight />}>
          {submitting ? 'Enviando' : 'Enviar chamado'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function TicketList({ tickets, role, user, onOpen, onNew }) {
  const { atos } = useTheme();
  const [status, setStatus] = useState('todos');
  const [produto, setProduto] = useState('todos');
  const [q, setQ] = useState('');
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const scoped = role === 'cliente' ? tickets.filter((t) => t.empresa === user.empresa) : tickets;
  const filtered = useMemo(() => scoped.filter((t) => {
    if (status !== 'todos' && t.status !== status) return false;
    if (produto !== 'todos' && t.produto !== produto) return false;
    if (q && !`${t.id} ${t.empresa} ${t.categoria} ${t.produto} ${t.assunto || ''}`.toLowerCase().includes(q.toLowerCase())) return false;
    return true;
  }), [scoped, status, produto, q]);

  useEffect(() => { setPage(0); }, [status, produto, q]);

  const openCount = scoped.filter(isOpenTicket).length;
  const inService = scoped.filter((t) => t.status === 'em_andamento').length;
  const riskCount = scoped.filter(isAtRisk).length;
  const visible = filtered.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
  const isClient = role === 'cliente';

  const activeFilters = [
    status !== 'todos' && { key: 'status', label: STATUS[status].label, clear: () => setStatus('todos') },
    produto !== 'todos' && { key: 'produto', label: produto, clear: () => setProduto('todos') },
    q && { key: 'q', label: `"${q}"`, clear: () => setQ('') },
  ].filter(Boolean);

  const refresh = () => { setRefreshing(true); setTimeout(() => setRefreshing(false), 500); };
  const head = { bgcolor: atos.tableHeaderBg, fontSize: 10, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.04em', color: 'text.secondary' };

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Stack direction="row" flexWrap="wrap" useFlexGap alignItems="center" justifyContent="space-between" spacing={2} mb={3}>
        <Box>
          <Eyebrow>{isClient ? 'Central de atendimento' : 'Operação de suporte'}</Eyebrow>
          <Typography variant="h1" sx={{ fontSize: 30 }}>{isClient ? 'Meus chamados' : 'Fila de atendimento'}</Typography>
          <Typography color="text.secondary" mt={1}>
            {isClient ? 'Acompanhe o andamento, o SLA e as atualizações dos seus chamados.' : 'Priorize os atendimentos pelo SLA e pela categoria do problema.'}
          </Typography>
        </Box>
        {isClient && onNew && <Button variant="contained" startIcon={<Add />} onClick={onNew}>Abrir novo chamado</Button>}
      </Stack>

      <Box component="section" aria-label="Indicadores dos chamados" sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 1.5, mb: 3 }}>
        <StatTile label="Total de chamados" value={scoped.length} detail={isClient ? 'na sua empresa' : 'todos os clientes'} tone="neutral" icon={ConfirmationNumberOutlined} />
        <StatTile label="Em aberto" value={openCount} tone="info" detail={`${inService} em atendimento`} icon={HeadsetMicOutlined} />
        <StatTile label="Próximos do SLA" value={riskCount} tone={riskCount ? 'error' : 'neutral'} detail="menos de 20% do prazo restante" icon={AccessTime} />
      </Box>

      <Stack direction="row" spacing={1.25} mb={1.25}>
        <TextField value={q} onChange={(e) => setQ(e.target.value)} placeholder="Buscar por número, empresa ou categoria" size="small" fullWidth type="search"
          inputProps={{ 'aria-label': 'Buscar chamados' }}
          InputProps={{ startAdornment: <InputAdornment position="start"><Search fontSize="small" /></InputAdornment> }}
          sx={{ '& .MuiOutlinedInput-root': { bgcolor: 'background.paper', height: 42 } }} />
        {[
          { label: 'Atualizar', icon: <Refresh fontSize="small" sx={refreshing ? { animation: 'spin 1s linear infinite', '@keyframes spin': { to: { transform: 'rotate(360deg)' } } } : null} />, onClick: refresh },
          { label: 'Filtros', icon: <Tune fontSize="small" />, onClick: () => setFiltersOpen((o) => !o), on: filtersOpen || activeFilters.length > 0 },
          { label: 'Exportar', icon: <FileDownloadOutlined fontSize="small" /> },
        ].map((b) => (
          <Tooltip key={b.label} title={b.label}>
            <IconButton aria-label={b.label} onClick={b.onClick} sx={{ width: 42, height: 42, borderRadius: 2, border: 1, borderColor: b.on ? 'primary.main' : 'divider', color: b.on ? 'primary.main' : 'text.secondary', bgcolor: 'background.paper', flexShrink: 0 }}>
              {b.icon}
            </IconButton>
          </Tooltip>
        ))}
      </Stack>

      <Collapse in={filtersOpen}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ p: 1.5, mb: 1.5, borderRadius: 2, bgcolor: atos.surfaceAlt, border: 1, borderColor: 'divider' }}>
          <TextField select size="small" label="Status" value={status} onChange={(e) => setStatus(e.target.value)} sx={{ minWidth: 200 }}>
            <MenuItem value="todos">Todos os status</MenuItem>
            {Object.entries(STATUS).map(([k, v]) => <MenuItem key={k} value={k}>{v.label}</MenuItem>)}
          </TextField>
          <TextField select size="small" label="Produto" value={produto} onChange={(e) => setProduto(e.target.value)} sx={{ minWidth: 200 }}>
            <MenuItem value="todos">Todos os produtos</MenuItem>
            {PRODUCTS.map((p) => <MenuItem key={p} value={p}>{p}</MenuItem>)}
          </TextField>
        </Stack>
      </Collapse>

      {activeFilters.length > 0 && (
        <Stack direction="row" flexWrap="wrap" useFlexGap spacing={1} justifyContent="flex-end" mb={1.5}>
          {activeFilters.map((f) => <Chip key={f.key} label={f.label} onDelete={f.clear} size="small" variant="outlined" />)}
        </Stack>
      )}

      {scoped.length === 0 || filtered.length === 0 ? (
        <Paper variant="outlined" sx={{ py: 8, textAlign: 'center' }}>
          {scoped.length === 0 ? <InboxOutlined sx={{ fontSize: 30, color: 'text.disabled' }} /> : <Search sx={{ fontSize: 28, color: 'text.disabled' }} />}
          <Typography sx={{ fontWeight: 500, mt: 1 }}>
            {scoped.length === 0 ? (isClient ? 'Você ainda não abriu nenhum chamado' : 'Nenhum chamado na fila') : 'Nenhum chamado encontrado'}
          </Typography>
          <Typography color="text.disabled" mt={0.5}>
            {scoped.length === 0 ? (isClient ? 'Quando precisar de ajuda, abra um chamado e acompanhe por aqui.' : 'Bom trabalho, tudo em dia.') : 'Tente buscar por outro número, empresa ou categoria.'}
          </Typography>
          {scoped.length === 0 && isClient && onNew && <Button variant="contained" startIcon={<Add />} onClick={onNew} sx={{ mt: 2 }}>Abrir novo chamado</Button>}
          {scoped.length > 0 && <Button variant="outlined" sx={{ mt: 2 }} onClick={() => { setStatus('todos'); setProduto('todos'); setQ(''); }}>Limpar filtros</Button>}
        </Paper>
      ) : (
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          <TableContainer>
            <Table size="medium" aria-label="Chamados">
              <TableHead>
                <TableRow>
                  <TableCell sx={head}>Chamado</TableCell>
                  <TableCell sx={head}>Categoria e produto</TableCell>
                  {!isClient && <TableCell sx={{ ...head, display: { xs: 'none', md: 'table-cell' } }}>Empresa</TableCell>}
                  <TableCell sx={head}>Prioridade</TableCell>
                  <TableCell sx={{ ...head, display: { xs: 'none', md: 'table-cell' } }}>Agente</TableCell>
                  <TableCell sx={head}>Status</TableCell>
                  <TableCell sx={head} width={32} />
                </TableRow>
              </TableHead>
              <TableBody>
                {visible.map((t, i) => (
                  <TableRow key={t.id} hover tabIndex={0} onClick={() => onOpen(t.id)} onKeyDown={(e) => { if (e.key === 'Enter') onOpen(t.id); }}
                    sx={{ cursor: 'pointer', bgcolor: i % 2 === 1 ? atos.tableRowAlt : 'background.paper', '& td': { py: 1.5 } }}>
                    <TableCell sx={{ borderLeft: 3, borderLeftColor: isAtRisk(t) ? 'error.main' : 'transparent', fontFamily: MONO, fontSize: 11, color: 'text.disabled', whiteSpace: 'nowrap' }}>{t.id}</TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>{t.categoria} · {t.produto}</TableCell>
                    {!isClient && <TableCell sx={{ display: { xs: 'none', md: 'table-cell' }, color: 'text.secondary' }}>{t.empresa}</TableCell>}
                    <TableCell><PriorityLabel level={t.prioridade} /></TableCell>
                    <TableCell sx={{ display: { xs: 'none', md: 'table-cell' }, color: 'text.secondary' }}>{t.agente || 'Sem agente'}</TableCell>
                    <TableCell><StatusChip status={t.status} /></TableCell>
                    <TableCell align="right"><ChevronRight fontSize="small" sx={{ color: 'text.disabled' }} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination component="div" count={filtered.length} page={page} onPageChange={(_, p) => setPage(p)}
            rowsPerPage={rowsPerPage} onRowsPerPageChange={(e) => { setRowsPerPage(Number(e.target.value)); setPage(0); }}
            rowsPerPageOptions={[5, 10, 25]} labelRowsPerPage="Linhas por página"
            labelDisplayedRows={({ from, to, count }) => `${from}–${to} de ${count}`}
            sx={{ borderTop: 1, borderColor: 'divider', '& .MuiTablePagination-toolbar': { minHeight: 48 } }} />
        </Paper>
      )}

      {isClient && scoped.length > 0 && <CustomerInsights tickets={scoped} />}
    </Box>
  );
}
