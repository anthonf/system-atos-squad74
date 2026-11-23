import { yupResolver } from '@hookform/resolvers/yup';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import * as yup from 'yup';
import type { SlaCategoriaDto } from '../../api';
import { ApiError, chamadoDtoParaTicket, criarChamado, listarSlaCategorias, TITULO_MAX } from '../../api';
import type { Dayjs } from '../../dates';
import { DATE_FORMAT, dayjs, formatDateTime } from '../../dates';
import i18n from '../../i18n';
import { notifyApiError } from '../../notify';
import type { Role, Session, StatusKey, Ticket } from '../../types';
import { isAtRisk, isOpenTicket } from '../ui';

const PRODUCT_NONE = '';

const newTicketSchema = yup.object({
  titulo: yup.string().trim().required('validation.required').max(TITULO_MAX, 'validation.titleMax'),
  produto: yup.string().required('validation.required'),
  categoria: yup.string().required('validation.required'),
  descricao: yup.string().trim().required('validation.required'),
});
export type NewTicketForm = yup.InferType<typeof newTicketSchema>;

export interface NewTicketModalProps {
  user: Session;
  onClose: () => void;
  onCreate: (ticket: Ticket) => void;
}

export interface TicketListProps {
  tickets: Ticket[];
  loading: boolean;
  error: boolean;
  onRefresh: () => Promise<void>;
  role: Role;
  user: Session;
  onOpen: (id: string) => void;
  onNew: () => void;
}

interface ActiveFilter { key: string; label: string; clear: () => void }

export function useApp({ tickets, loading, error, onRefresh, role, user, onNew }: TicketListProps) {
  const { atos } = useTheme();
  const { t } = useTranslation();
  const [status, setStatus] = useState<StatusKey | 'todos'>('todos');
  const [produto, setProduto] = useState('todos');
  const [q, setQ] = useState('');
  const [dateFrom, setDateFrom] = useState<Dayjs | null>(null);
  const [dateTo, setDateTo] = useState<Dayjs | null>(null);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const scoped = role === 'cliente' ? tickets.filter((t) => t.empresa === user.empresa) : tickets;
  const filtered = useMemo(() => scoped.filter((x) => {
    if (status !== 'todos' && x.status !== status) return false;
    if (produto !== 'todos' && x.produto !== produto) return false;
    if (q && !`${x.id} ${x.empresa} ${x.categoria} ${x.produto} ${x.assunto || ''}`.toLowerCase().includes(q.toLowerCase())) return false;
    if (dateFrom && dayjs(x.abertura).isBefore(dateFrom.startOf('day'))) return false;
    if (dateTo && dayjs(x.abertura).isAfter(dateTo.endOf('day'))) return false;
    return true;
  }), [scoped, status, produto, q, dateFrom, dateTo]);

  useEffect(() => { setPage(0); }, [status, produto, q, dateFrom, dateTo]);

  const openCount = scoped.filter(isOpenTicket).length;
  const inService = scoped.filter((x) => x.status === 'em_andamento').length;
  const riskCount = scoped.filter(isAtRisk).length;
  const visible = filtered.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
  const isClient = role === 'cliente';
  const produtos = useMemo(() => [...new Set(tickets.map((x) => x.produto))].sort(), [tickets]);
  const canOpen = Boolean(user.grupoEmpresaId) && (isClient || role === 'admin');

  const activeFilters: ActiveFilter[] = [];
  if (status !== 'todos') activeFilters.push({ key: 'status', label: t(`status.${status}`), clear: () => setStatus('todos') });
  if (produto !== 'todos') activeFilters.push({ key: 'produto', label: produto, clear: () => setProduto('todos') });
  if (q) activeFilters.push({ key: 'q', label: `"${q}"`, clear: () => setQ('') });
  if (dateFrom) activeFilters.push({ key: 'from', label: `${t('tickets.openedFrom')}: ${dateFrom.format(DATE_FORMAT)}`, clear: () => setDateFrom(null) });
  if (dateTo) activeFilters.push({ key: 'to', label: `${t('tickets.openedTo')}: ${dateTo.format(DATE_FORMAT)}`, clear: () => setDateTo(null) });

  const refresh = useCallback(() => onRefresh(), [onRefresh]);

  const clearFilters = () => { setStatus('todos'); setProduto('todos'); setQ(''); setDateFrom(null); setDateTo(null); };
  const changeRowsPerPage = (value: number) => { setRowsPerPage(value); setPage(0); };

  return {
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
  };
}

export function useNewTicketModal({ user, onCreate }: NewTicketModalProps) {
  const { t } = useTranslation();
  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.down('sm')); // celular: modal em tela cheia
  const [opcoes, setOpcoes] = useState<SlaCategoriaDto[]>([]);   // pares produto/categoria vindos da API
  const [carregando, setCarregando] = useState(true);
  const [erroOpcoes, setErroOpcoes] = useState(false);

  const { control, handleSubmit, setError, setValue, watch, formState: { isSubmitting } } = useForm<NewTicketForm>({
    resolver: yupResolver(newTicketSchema),
    mode: 'onTouched',
    defaultValues: { titulo: '', produto: PRODUCT_NONE, categoria: '', descricao: '' },
  });
  const produto = watch('produto');
  const titulo = watch('titulo');

  const carregarOpcoes = () => {
    setCarregando(true);
    setErroOpcoes(false);
    listarSlaCategorias()
      .then((lista) => {
        setOpcoes(lista);
        setValue('produto', lista[0]?.produto ?? PRODUCT_NONE);
        setValue('categoria', '');
      })
      .catch((e: unknown) => { setErroOpcoes(true); notifyApiError(e, i18n.t('newTicket.loadError')); })
      .finally(() => setCarregando(false));
  };
  useEffect(carregarOpcoes, []); // eslint-disable-line react-hooks/exhaustive-deps

  const produtos = useMemo(() => [...new Set(opcoes.map((o) => o.produto))], [opcoes]);
  const categorias = useMemo(() => opcoes.filter((o) => o.produto === produto).map((o) => o.categoria), [opcoes, produto]);

  const onSubmit = async (data: NewTicketForm) => {
    try {
      const criado = await criarChamado({ titulo: data.titulo, produto: data.produto, categoria: data.categoria, descricao: data.descricao });
      toast.success(t('newTicket.success', { id: criado.codigoPublico }));
      onCreate(chamadoDtoParaTicket(criado, user));
    } catch (err) {
      // A API recusou o título (400 com campo "titulo"): mostra a mensagem dela sob o campo, sem fechar o formulário.
      if (err instanceof ApiError && err.status === 400 && err.campo === 'titulo') {
        setError('titulo', { type: 'server', message: err.message }, { shouldFocus: true });
        return;
      }
      notifyApiError(err, t('newTicket.submitError'));
    }
  };

  return { t, fullScreen, carregando, erroOpcoes, control, handleSubmit, setValue, isSubmitting, titulo, produtos, categorias, onSubmit, carregarOpcoes };
}

export function useCustomerInsights({ tickets }: { tickets: Ticket[] }) {
  const { atos } = useTheme();
  const { t } = useTranslation();
  const doing = tickets.filter((x) => ['aberto', 'em_andamento'].includes(x.status)).length;
  const waiting = tickets.filter((x) => x.status === 'aguardando_cliente').length;
  const done = tickets.filter((x) => ['resolvido', 'fechado'].includes(x.status)).length;
  const groups = [
    { label: t('tickets.insights.inService'), value: doing, color: atos.warning },
    { label: t('tickets.insights.waitingYou'), value: waiting, color: atos.purple },
    { label: t('tickets.insights.done'), value: done, color: atos.textMuted },
  ];
  const total = Math.max(1, doing + waiting + done);
  const updates = tickets.slice(0, 2).map((x) => {
    const last = x.mensagens[x.mensagens.length - 1];
    const name = last ? last.autor.split(' ')[0] : '';
    return {
      id: x.id,
      titulo: last ? t(last.tipo === 'agente' ? 'tickets.insights.replied' : 'tickets.insights.wrote', { name }) : t('tickets.insights.registered'),
      quando: formatDateTime(last ? last.data : x.abertura),
      agente: Boolean(last && last.tipo === 'agente'),
    };
  });

  return { atos, t, groups, total, updates };
}
