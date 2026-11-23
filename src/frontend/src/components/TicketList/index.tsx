import {
  useApp,
  useCustomerInsights,
  useNewTicketModal,
  type NewTicketForm,
  type NewTicketModalProps,
  type TicketListProps,
} from './useApp';
// Fila / "Meus chamados": indicadores, busca, filtros (inclusive por data), tabela paginada, resumo do cliente e modal de novo chamado.
import {
  AccessTime,
  Add,
  BarChartOutlined,
  Cancel,
  CheckCircleOutline,
  ChevronRight,
  Close,
  ConfirmationNumberOutlined,
  FileDownloadOutlined,
  HeadsetMicOutlined,
  InboxOutlined,
  Refresh,
  Search,
  Tune,
} from '@mui/icons-material';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  InputAdornment,
  Link,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import React from 'react';
import { TITULO_MAX } from '../../api';
import { STATUS } from '../../data';
import type { Dayjs } from '../../dates';
import { DATE_FORMAT } from '../../dates';
import { MONO } from '../../tokens';
import type { StatusKey, Ticket } from '../../types';
import FormTextField from '../form/FormTextField';
import { Eyebrow, isAtRisk, PriorityLabel, StatTile, StatusChip, visuallyHidden } from '../ui';

function CustomerInsights({ tickets }: { tickets: Ticket[] }) {
  const { atos, t, groups, total, updates } = useCustomerInsights({ tickets });

  return (
    <Box component="section" aria-label={t('tickets.insights.region')} sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '2fr 3fr' }, gap: 2, mt: 3 }}>
      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box><Eyebrow>{t('tickets.insights.overview')}</Eyebrow><Typography variant="h2">{t('tickets.insights.byStatus')}</Typography></Box>
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
        <Eyebrow>{t('tickets.insights.followUp')}</Eyebrow>
        <Typography variant="h2">{t('tickets.insights.recentUpdates')}</Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 1.75, mt: 2 }}>
          {updates.length === 0 && <Typography color="text.disabled">{t('tickets.insights.none')}</Typography>}
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

export function NewTicketModal(props: NewTicketModalProps) {
  const { onClose } = props;
  const { t, fullScreen, carregando, erroOpcoes, control, handleSubmit, setValue, isSubmitting, titulo, produtos, categorias, onSubmit, carregarOpcoes } = useNewTicketModal(props);

  return (
    <Dialog open onClose={isSubmitting ? undefined : onClose} fullWidth maxWidth="xs" fullScreen={fullScreen} aria-labelledby="new-ticket-title"
      PaperProps={{ component: 'form', onSubmit: handleSubmit(onSubmit), noValidate: true }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
          <Box><Eyebrow>{t('newTicket.eyebrow')}</Eyebrow><Typography id="new-ticket-title" variant="h2" sx={{ fontSize: 18 }}>{t('newTicket.title')}</Typography></Box>
          <IconButton aria-label={t('common.close')} onClick={onClose} disabled={isSubmitting}><Close /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        {carregando ? (
          <Stack alignItems="center" spacing={1.5} sx={{ py: 5 }}>
            <CircularProgress size={24} />
            <Typography variant="body2" color="text.secondary">{t('newTicket.loadingOptions')}</Typography>
          </Stack>
        ) : erroOpcoes ? (
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Typography color="text.secondary">{t('newTicket.loadError')}</Typography>
            <Button variant="outlined" startIcon={<Refresh />} onClick={carregarOpcoes} sx={{ alignSelf: 'flex-start' }}>{t('common.tryAgain')}</Button>
          </Stack>
        ) : (
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Box>
              <FormTextField<NewTicketForm> name="titulo" control={control} required autoFocus fullWidth size="small"
                label={t('newTicket.fields.title')} placeholder={t('newTicket.fields.titlePlaceholder')}
                inputProps={{ maxLength: TITULO_MAX + 50 }} errorParams={{ max: TITULO_MAX }} />
              <Typography variant="caption" component="div" sx={{ textAlign: 'right', mt: 0.25 }}>{titulo.length}/{TITULO_MAX}</Typography>
            </Box>
            <FormTextField<NewTicketForm> name="produto" control={control} select required fullWidth size="small"
              label={t('newTicket.fields.product')} onValueChange={() => setValue('categoria', '')}>
              {produtos.map((p) => <MenuItem key={p} value={p}>{p}</MenuItem>)}
            </FormTextField>
            <FormTextField<NewTicketForm> name="categoria" control={control} select required fullWidth size="small"
              label={t('newTicket.fields.category')}>
              {categorias.map((c) => <MenuItem key={c} value={c}>{c}</MenuItem>)}
            </FormTextField>
            <FormTextField<NewTicketForm> name="descricao" control={control} required multiline minRows={4} fullWidth size="small"
              label={t('newTicket.fields.description')} placeholder={t('newTicket.fields.descriptionPlaceholder')} />
          </Stack>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button variant="outlined" onClick={onClose} disabled={isSubmitting}>{t('common.cancel')}</Button>
        <Button type="submit" variant="contained" disabled={isSubmitting || carregando || erroOpcoes}
          startIcon={isSubmitting ? <CircularProgress size={14} color="inherit" /> : null} endIcon={!isSubmitting && <ChevronRight />}>
          {isSubmitting ? t('newTicket.submitting') : t('newTicket.submit')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

export default function TicketList(props: TicketListProps) {
  const { onOpen, onNew } = props;
  const {
    atos,
    t,
    status,
    setStatus,
    produto,
    setProduto,
    q,
    setQ,
    dateFrom,
    setDateFrom,
    dateTo,
    setDateTo,
    filtersOpen,
    setFiltersOpen,
    loading,
    error,
    produtos,
    page,
    setPage,
    rowsPerPage,
    scoped,
    filtered,
    openCount,
    inService,
    riskCount,
    visible,
    isClient,
    canOpen,
    activeFilters,
    refresh,
    clearFilters,
    changeRowsPerPage,
  } = useApp(props);
  const head = { bgcolor: atos.tableHeaderBg, fontSize: 11, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.04em', color: 'text.secondary' };


  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Stack direction="row" flexWrap="wrap" useFlexGap alignItems="center" justifyContent="space-between" spacing={2} mb={3}>
        <Box>
          <Eyebrow>{isClient ? t('tickets.eyebrowClient') : t('tickets.eyebrowTeam')}</Eyebrow>
          <Typography variant="h1" sx={{ fontSize: 30 }}>{isClient ? t('tickets.titleClient') : t('tickets.titleTeam')}</Typography>
          <Typography color="text.secondary" mt={1}>
            {isClient ? t('tickets.subtitleClient') : t('tickets.subtitleTeam')}
          </Typography>
        </Box>
        {canOpen && <Button variant="contained" startIcon={<Add />} onClick={onNew}>{t('tickets.openNew')}</Button>}
      </Stack>

      <Box component="section" aria-label={t('tickets.statsRegion')} sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, 1fr)' }, gap: 1.5, mb: 3 }}>
        <StatTile label={t('tickets.total')} value={scoped.length} detail={isClient ? t('tickets.totalClient') : t('tickets.totalTeam')} tone="neutral" icon={ConfirmationNumberOutlined} />
        <StatTile label={t('tickets.open')} value={openCount} tone="info" detail={t('tickets.inService', { n: inService })} icon={HeadsetMicOutlined} />
        <StatTile label={t('tickets.nearSla')} value={riskCount} tone={riskCount ? 'error' : 'neutral'} detail={t('tickets.nearSlaDetail')} icon={AccessTime} />
      </Box>

      <Box role="status" sx={visuallyHidden}>{t('tickets.resultsCount', { n: filtered.length })}</Box>

      <Stack direction="row" spacing={1.25} mb={1.25}>
        <TextField value={q} onChange={(e) => setQ(e.target.value)} placeholder={t('tickets.searchPlaceholder')} size="small" fullWidth type="search"
          inputProps={{ 'aria-label': t('tickets.searchAria') }}
          InputProps={{ startAdornment: <InputAdornment position="start"><Search fontSize="small" /></InputAdornment> }}
          sx={{ '& .MuiOutlinedInput-root': { bgcolor: 'background.paper', height: 42 } }} />
        {[
          { label: t('tickets.refresh'), icon: <Refresh fontSize="small" sx={loading ? { animation: 'spin 1s linear infinite', '@keyframes spin': { to: { transform: 'rotate(360deg)' } } } : null} />, onClick: refresh },
          { label: t('tickets.filters'), icon: <Tune fontSize="small" />, onClick: () => setFiltersOpen((o) => !o), on: filtersOpen || activeFilters.length > 0, expanded: filtersOpen },
          { label: t('tickets.export'), icon: <FileDownloadOutlined fontSize="small" /> },
        ].map((b) => (
          <Tooltip key={b.label} title={b.label}>
            <IconButton aria-label={b.label} aria-expanded={b.expanded} onClick={b.onClick} sx={{ width: 42, height: 42, borderRadius: 2, border: 1, borderColor: b.on ? 'primary.main' : 'divider', color: b.on ? 'primary.main' : 'text.secondary', bgcolor: 'background.paper', flexShrink: 0 }}>
              {b.icon}
            </IconButton>
          </Tooltip>
        ))}
      </Stack>

      <Collapse in={filtersOpen}>
        <Stack direction={{ xs: 'column', sm: 'row' }} flexWrap="wrap" useFlexGap spacing={1.5} sx={{ p: 1.5, mb: 1.5, borderRadius: 2, bgcolor: atos.surfaceAlt, border: 1, borderColor: 'divider' }}>
          <TextField select size="small" label={t('tickets.filterStatus')} value={status} onChange={(e) => setStatus(e.target.value as StatusKey | 'todos')} sx={{ minWidth: 200 }}>
            <MenuItem value="todos">{t('tickets.allStatus')}</MenuItem>
            {(Object.keys(STATUS) as StatusKey[]).map((k) => <MenuItem key={k} value={k}>{t(`status.${k}`)}</MenuItem>)}
          </TextField>
          <TextField select size="small" label={t('tickets.filterProduct')} value={produto} onChange={(e) => setProduto(e.target.value)} sx={{ minWidth: 200 }}>
            <MenuItem value="todos">{t('tickets.allProducts')}</MenuItem>
            {produtos.map((p) => <MenuItem key={p} value={p}>{p}</MenuItem>)}
          </TextField>
          <DatePicker label={t('tickets.openedFrom')} value={dateFrom} onChange={(v: Dayjs | null) => setDateFrom(v)} format={DATE_FORMAT}
            maxDate={dateTo ?? undefined} slotProps={{ textField: { size: 'small', sx: { minWidth: 200 } }, field: { clearable: true, onClear: () => setDateFrom(null) } }} />
          <DatePicker label={t('tickets.openedTo')} value={dateTo} onChange={(v: Dayjs | null) => setDateTo(v)} format={DATE_FORMAT}
            minDate={dateFrom ?? undefined} slotProps={{ textField: { size: 'small', sx: { minWidth: 200 } }, field: { clearable: true, onClear: () => setDateTo(null) } }} />
        </Stack>
      </Collapse>

      {activeFilters.length > 0 && (
        <Stack direction="row" flexWrap="wrap" useFlexGap spacing={1} justifyContent="flex-end" mb={1.5}>
          {activeFilters.map((f) => <Chip key={f.key} label={f.label} onDelete={f.clear} deleteIcon={<Cancel titleAccess={t('tickets.removeFilter', { name: f.label })} />} size="small" variant="outlined" />)}
        </Stack>
      )}

      {error && (
        <Paper variant="outlined" sx={{ p: 2, mb: 1.5, borderColor: 'error.main', bgcolor: 'error.light', color: 'error.dark' }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} alignItems={{ sm: 'center' }} justifyContent="space-between" spacing={1}>
            <Typography variant="body2">{t('tickets.loadError')}</Typography>
            <Button size="small" variant="outlined" color="inherit" startIcon={<Refresh />} onClick={refresh}>{t('common.tryAgain')}</Button>
          </Stack>
        </Paper>
      )}
      {loading && scoped.length === 0 && !error ? (
        <Paper variant="outlined" sx={{ py: 6, textAlign: 'center' }}>
          <CircularProgress size={24} />
          <Typography color="text.secondary" mt={1.5}>{t('common.loading')}</Typography>
        </Paper>
      ) : scoped.length === 0 || filtered.length === 0 ? (
        <Paper variant="outlined" sx={{ py: 8, textAlign: 'center' }}>
          {scoped.length === 0 ? <InboxOutlined sx={{ fontSize: 30, color: 'text.disabled' }} /> : <Search sx={{ fontSize: 28, color: 'text.disabled' }} />}
          <Typography sx={{ fontWeight: 500, mt: 1 }}>
            {scoped.length === 0 ? (isClient ? t('tickets.emptyClientTitle') : t('tickets.emptyTeamTitle')) : t('tickets.notFoundTitle')}
          </Typography>
          <Typography color="text.disabled" mt={0.5}>
            {scoped.length === 0 ? (isClient ? t('tickets.emptyClientText') : t('tickets.emptyTeamText')) : t('tickets.notFoundText')}
          </Typography>
          {scoped.length === 0 && canOpen && <Button variant="contained" startIcon={<Add />} onClick={onNew} sx={{ mt: 2 }}>{t('tickets.openNew')}</Button>}
          {scoped.length > 0 && <Button variant="outlined" sx={{ mt: 2 }} onClick={clearFilters}>{t('tickets.clearFilters')}</Button>}
        </Paper>
      ) : (
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
          <TableContainer>
            <Table size="medium" aria-label={t('tickets.table.label')} sx={{ '& th, & td': { px: { xs: 1, sm: 2 } } }}>
              <TableHead>
                <TableRow>
                  <TableCell sx={head}>{t('tickets.table.ticket')}</TableCell>
                  <TableCell sx={head}>{t('tickets.table.categoryProduct')}</TableCell>
                  {!isClient && <TableCell sx={{ ...head, display: { xs: 'none', md: 'table-cell' } }}>{t('tickets.table.company')}</TableCell>}
                  <TableCell sx={{ ...head, display: { xs: 'none', sm: 'table-cell' } }}>{t('tickets.table.priority')}</TableCell>
                  <TableCell sx={{ ...head, display: { xs: 'none', md: 'table-cell' } }}>{t('tickets.table.agent')}</TableCell>
                  <TableCell sx={head}>{t('tickets.table.status')}</TableCell>
                  <TableCell sx={{ ...head, display: { xs: 'none', sm: 'table-cell' } }} width={32} />
                </TableRow>
              </TableHead>
              <TableBody>
                {visible.map((tk, i) => (
                  <TableRow key={tk.id} hover onClick={() => onOpen(tk.id)}
                    sx={{ cursor: 'pointer', bgcolor: i % 2 === 1 ? atos.tableRowAlt : 'background.paper', '& td': { py: 1.5 } }}>
                    <TableCell sx={{ borderLeft: 3, borderLeftColor: isAtRisk(tk) ? 'error.main' : 'transparent', whiteSpace: 'nowrap' }}>
                      {/* Botão real: alcançável por teclado (Tab/Enter/Espaço) e anunciado por leitores de tela. */}
                      <Link component="button" type="button" underline="hover" onClick={(e: React.MouseEvent) => { e.stopPropagation(); onOpen(tk.id); }}
                        aria-label={t('tickets.openTicket', { id: tk.id })} sx={{ fontFamily: MONO, fontSize: 12, color: 'text.secondary', textAlign: 'left' }}>{tk.id}</Link>
                    </TableCell>
                    <TableCell sx={{ fontWeight: 600 }}>
                      {tk.assunto || `${tk.categoria} · ${tk.produto}`}
                      {tk.assunto && <Typography variant="caption" component="div" sx={{ fontWeight: 400 }}>{tk.categoria} · {tk.produto}</Typography>}
                      <Box sx={{ display: { xs: 'block', sm: 'none' }, mt: 0.5 }}><PriorityLabel level={tk.prioridade} /></Box>
                    </TableCell>
                    {!isClient && <TableCell sx={{ display: { xs: 'none', md: 'table-cell' }, color: 'text.secondary' }}>{tk.empresa}</TableCell>}
                    <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}><PriorityLabel level={tk.prioridade} /></TableCell>
                    <TableCell sx={{ display: { xs: 'none', md: 'table-cell' }, color: 'text.secondary' }}>{tk.agente || t('tickets.table.noAgent')}</TableCell>
                    <TableCell><StatusChip status={tk.status} /></TableCell>
                    <TableCell align="right" sx={{ display: { xs: 'none', sm: 'table-cell' } }}><ChevronRight fontSize="small" aria-hidden sx={{ color: 'text.disabled' }} /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination component="div" count={filtered.length} page={page} onPageChange={(_, p) => setPage(p)}
            rowsPerPage={rowsPerPage} onRowsPerPageChange={(e) => changeRowsPerPage(Number(e.target.value))}
            rowsPerPageOptions={[5, 10, 25]} labelRowsPerPage={t('tickets.rowsPerPage')}
            getItemAriaLabel={(type: 'first' | 'last' | 'next' | 'previous') => t(`tickets.pagination.${type}`)}
            labelDisplayedRows={({ from, to, count }) => t('tickets.displayedRows', { from, to, total: count })}
            sx={{ borderTop: 1, borderColor: 'divider', '& .MuiTablePagination-toolbar': { minHeight: 48 } }} />
        </Paper>
      )}

      {isClient && scoped.length > 0 && <CustomerInsights tickets={scoped} />}
    </Box>
  );
}
