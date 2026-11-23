import { useCallback, useEffect, useMemo, useRef, useState, useSyncExternalStore } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { chamadoDtoParaTicket, consultarChamado, listarChamados } from '../api';
import { getAuthState, logout as logoutSession, restoreSession, subscribeAuth } from '../auth';
import type { Lang } from '../i18n';
import i18n, { LANGS } from '../i18n';
import { notifyApiError } from '../notify';
import { ApiError } from '../http';
import { buildTheme } from '../theme';
import type { AppNotification, Session, ThemeMode, Ticket } from '../types';

export interface TicketDetailRouteProps {
  tickets: Ticket[];
  session: Session;
  onUpdate: (ticket: Ticket) => void;
}

export function useApp() {
  const { t: tr, i18n: i18nInstance } = useTranslation();
  const lang: Lang = (LANGS as readonly string[]).includes(i18nInstance.language) ? (i18nInstance.language as Lang) : 'pt';
  const location = useLocation();
  const navigate = useNavigate();
  const activationSuccess = Boolean((location.state as { activationSuccess?: boolean } | null)?.activationSuccess);
  const [themeMode, changeTheme] = useState<ThemeMode>(() => {
    try { return localStorage.getItem('atos.theme') === 'dark' ? 'dark' : 'light'; }
    catch { return 'light'; }
  });
  const setThemeMode = (mode: ThemeMode) => {
    changeTheme(mode);
    try { localStorage.setItem('atos.theme', mode); } catch { /* Storage may be disabled. */ }
  };
  const theme = useMemo(() => buildTheme(themeMode), [themeMode]);

  const { session, status: authStatus } = useSyncExternalStore(subscribeAuth, getAuthState);
  useEffect(() => { void restoreSession(); }, []);
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [ticketsLoading, setTicketsLoading] = useState(true);
  const [ticketsError, setTicketsError] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [showNewTicket, setShowNewTicket] = useState(false);
  const unreadCount = notifications.filter((n) => !n.lida).length;

  const carregarChamados = useCallback(async () => {
    if (!session) return;
    setTicketsLoading(true);
    setTicketsError(false);
    try {
      const filtros = session.role === 'cliente' && session.grupoEmpresaId
        ? { grupoEmpresaId: session.grupoEmpresaId }
        : {};
      const lista = await listarChamados(filtros);
      setTickets(lista.map((dto) => chamadoDtoParaTicket(dto, session)));
    } catch (error) {
      setTicketsError(true);
      notifyApiError(error, tr('tickets.loadError'));
    } finally {
      setTicketsLoading(false);
    }
  }, [session, tr]);

  useEffect(() => {
    if (!session) { setTickets([]); setTicketsLoading(false); return; }
    void carregarChamados();
  }, [session, carregarChamados]);

  const markAllRead = () => setNotifications((prev) => prev.map((n) => ({ ...n, lida: true })));
  const markOneRead = (id: number) => setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)));

  const updateTicket = useCallback((updated: Ticket) => setTickets((prev) => prev.some((t) => t.id === updated.id)
    ? prev.map((t) => (t.id === updated.id ? updated : t))
    : [...prev, updated]), []);
  const createTicket = (t: Ticket) => {
    setTickets((prev) => [t, ...prev]);
    setNotifications((prev) => [{ id: Date.now(), texto: i18n.t('notifications.ticketRegistered', { id: t.id }), data: i18n.t('notifications.now'), lida: false }, ...prev]);
    setShowNewTicket(false);
  };
  const login = (_session: Session) => {
    if (location.pathname === '/' || location.pathname === '/login') navigate('/chamados', { replace: true });
  };
  const logout = () => {
    setShowNewTicket(false);
    navigate('/login', { replace: true });
    void logoutSession().catch(error => notifyApiError(error, tr('api.generic')));
  };

  const mainRef = useRef<HTMLElement>(null);
  useEffect(() => {
    if (!session) return;
    setMobileNavOpen(false);
    mainRef.current?.focus({ preventScroll: true });
  }, [location.pathname, session]);

  return {
    tr,
    lang,
    location,
    navigate,
    activationSuccess,
    themeMode,
    setThemeMode,
    theme,
    session,
    authStatus,
    tickets,
    ticketsLoading,
    ticketsError,
    carregarChamados,
    mobileNavOpen,
    setMobileNavOpen,
    notifications,
    showNewTicket,
    setShowNewTicket,
    unreadCount,
    markAllRead,
    markOneRead,
    updateTicket,
    createTicket,
    login,
    logout,
    mainRef,
  };
}

export function useTicketDetailRoute({ tickets, session, onUpdate }: TicketDetailRouteProps) {
  const { t: tr } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [notFound, setNotFound] = useState(false);
  const recarregar = useCallback(async () => {
    if (!id) return;
    setLoading(true);
    setError(false);
    setNotFound(false);
    try {
      const detalhe = await consultarChamado(id);
      onUpdate(chamadoDtoParaTicket(detalhe.chamado, session, detalhe.interacoes));
    } catch (err) {
      setNotFound(err instanceof ApiError && err.status === 404);
      setError(!(err instanceof ApiError && err.status === 404));
      notifyApiError(err, tr('detail.loadError'));
    } finally {
      setLoading(false);
    }
  }, [id, session, onUpdate, tr]);

  useEffect(() => { void recarregar(); }, [recarregar]);

  const ticket = tickets.find((x) => x.id === id);
  const canView = Boolean(ticket && (session.role !== 'cliente' || (session.grupoEmpresaId != null && ticket.grupoEmpresaId === session.grupoEmpresaId)));
  const backToList = () => navigate('/chamados');
  return { ticket, canView, loading, error, notFound, recarregar, backToList };
}
