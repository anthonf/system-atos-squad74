// Aplicação: estado global (sessão, chamados, notificações), tema MUI e rotas (React Router) com navegação por papel.
import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Box, CssBaseline, Skeleton, Stack, ThemeProvider } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { enUS, esES, ptBR } from '@mui/x-date-pickers/locales';
import { useTranslation } from 'react-i18next';
import { Navigate, Route, Routes, useLocation, useNavigate, useParams } from 'react-router-dom';
import { Toaster } from 'sonner';
import { visuallyHidden } from './components/ui';
import { buildTheme } from './theme';
import { seed, seedNotifications } from './data';
import i18n, { DAYJS_LOCALE, LANGS } from './i18n';
import { clearToken } from './api';
import type { Lang } from './i18n';
import LoginScreen from './components/Login';
import { Sidebar, TopBar } from './components/Layout';
import TicketList, { NewTicketModal } from './components/TicketList';
import TicketDetail from './components/TicketDetail';
import Reports from './components/Reports';
import UserAdmin from './components/UserAdmin';
import type { AppNotification, Role, Session, ThemeMode, Ticket } from './types';

// Textos dos seletores de data (x-date-pickers) por idioma da interface.
const PICKER_TEXT = {
  pt: ptBR.components.MuiLocalizationProvider.defaultProps.localeText,
  en: enUS.components.MuiLocalizationProvider.defaultProps.localeText,
  es: esES.components.MuiLocalizationProvider.defaultProps.localeText,
};

function TicketPage({ tickets, session, onUpdate }: {
  tickets: Ticket[];
  session: Session;
  onUpdate: (ticket: Ticket) => void;
}) {
  const { id } = useParams();
  const navigate = useNavigate();
  const ticket = tickets.find((item) => item.id === id);
  if (!ticket || (session.role === 'cliente' && ticket.empresa !== session.empresa)) {
    return <Navigate to="/chamados" replace />;
  }
  return <TicketDetail key={ticket.id} ticket={ticket} role={session.role} user={session}
    onBack={() => navigate('/chamados')} onUpdate={onUpdate} />;
}

interface TicketDetailRouteProps {
  tickets: Ticket[];
  session: Session;
  onUpdate: (ticket: Ticket) => void;
}

/** /chamados/:id — cliente só abre chamado da própria empresa; id inexistente volta para a lista. */
function TicketDetailRoute({ tickets, session, onUpdate }: TicketDetailRouteProps) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const ticket = tickets.find((x) => x.id === id);
  if (!ticket || (session.role === 'cliente' && ticket.empresa !== session.empresa)) return <Navigate to="/chamados" replace />;
  return <TicketDetail ticket={ticket} role={session.role} user={session} onBack={() => navigate('/chamados')} onUpdate={onUpdate} />;
}

/** Rota restrita a alguns papéis; os demais voltam para a lista de chamados. */
function RoleRoute({ allow, role, children }: { allow: Role[]; role: Role; children: React.ReactElement }) {
  return allow.includes(role) ? children : <Navigate to="/chamados" replace />;
}

/** Placeholder exibido enquanto a view da rota carrega. */
function ViewSkeleton() {
  return (
    <Box sx={{ maxWidth: 1152 }} aria-hidden>
      <Skeleton width={220} height={34} />
      <Skeleton width={320} height={22} sx={{ mb: 4 }} />
      <Stack spacing={1.5}>{[0, 1, 2, 3, 4].map((i) => <Skeleton key={i} variant="rounded" height={48} />)}</Stack>
    </Box>
  );
}

export default function SupportPortal() {
  const { t: tr, i18n: i18nInstance } = useTranslation();
  const lang: Lang = (LANGS as readonly string[]).includes(i18nInstance.language) ? (i18nInstance.language as Lang) : 'pt';
  const location = useLocation();
  const navigate = useNavigate();
  const [themeMode, changeTheme] = useState<ThemeMode>(() => {
    try { return localStorage.getItem('atos.theme') === 'dark' ? 'dark' : 'light'; }
    catch { return 'light'; }
  });
  const setThemeMode = (mode: ThemeMode) => {
    changeTheme(mode);
    try { localStorage.setItem('atos.theme', mode); } catch { /* Storage may be disabled. */ }
  };
  const theme = useMemo(() => buildTheme(themeMode), [themeMode]);

  const [session, setSession] = useState<Session | null>(null);
  const [tickets, setTickets] = useState<Ticket[]>(seed);
  const [loadingView, setLoadingView] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [notifications, setNotifications] = useState<AppNotification[]>(seedNotifications);
  const [showNewTicket, setShowNewTicket] = useState(false);
  const unreadCount = notifications.filter((n) => !n.lida).length;

  const markAllRead = () => setNotifications((prev) => prev.map((n) => ({ ...n, lida: true })));
  const markOneRead = (id: number) => setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)));

  const updateTicket = (updated: Ticket) => setTickets((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
  const createTicket = (t: Ticket) => {
    setTickets((prev) => [t, ...prev]);
    setNotifications((prev) => [{ id: Date.now(), texto: i18n.t('notifications.ticketRegistered', { id: t.id }), data: i18n.t('notifications.now'), lida: false }, ...prev]);
    setShowNewTicket(false);
  };
  const login = (s: Session) => {
    setSession(s);
    if (location.pathname === '/') navigate('/chamados', { replace: true });
  };
  const logout = () => { clearToken(); setSession(null); setShowNewTicket(false); navigate('/', { replace: true }); };

  const role = session?.role;
  const mainRef = useRef<HTMLElement>(null);
  // Troca de rota: fecha o menu, mostra o carregamento e leva o foco para o conteúdo (leitores de tela e teclado).
  useEffect(() => {
    if (!session) return undefined;
    setMobileNavOpen(false);
    setLoadingView(true);
    const timer = window.setTimeout(() => setLoadingView(false), 400);
    mainRef.current?.focus({ preventScroll: true });
    return () => window.clearTimeout(timer);
  }, [location.pathname, session]);

  return (
    <ThemeProvider theme={theme}>
      <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale={DAYJS_LOCALE[lang]} localeText={PICKER_TEXT[lang]}>
      <CssBaseline />
      {!session ? (
        <Routes>
          <Route path="/login" element={<LoginScreen onLogin={login} themeMode={themeMode} setThemeMode={setThemeMode} />} />
          <Route path="*" element={<Navigate to="/login" replace state={{ from: location.pathname }} />} />
        </Routes>
      ) : (
        <Box sx={{ display: 'flex', height: '100vh', '@supports (height: 100dvh)': { height: '100dvh' }, overflow: 'hidden', bgcolor: 'background.default' }}>
          <Box component="a" href="#main-content"
            sx={{ ...visuallyHidden, '&:focus': { position: 'fixed', top: 8, left: 8, zIndex: 2000, width: 'auto', height: 'auto', margin: 0, clip: 'auto', overflow: 'visible', p: '10px 16px', borderRadius: 1, bgcolor: 'background.paper', color: 'text.primary', fontWeight: 600, border: 2, borderColor: 'primary.main' } }}>
            {tr('a11y.skipToContent')}
          </Box>
          <Sidebar
            session={session} onLogout={logout}
            themeMode={themeMode} setThemeMode={setThemeMode}
            mobileOpen={mobileNavOpen} onMobileClose={() => setMobileNavOpen(false)}
          />
          <Box sx={{ flex: 1, minWidth: 0, height: '100%', overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
            <TopBar
              session={session} themeMode={themeMode} notifications={notifications} unreadCount={unreadCount}
              onOpenNav={() => setMobileNavOpen(true)} onMarkAllRead={markAllRead} onMarkOneRead={markOneRead}
            />
            <Box component="main" id="main-content" ref={mainRef} tabIndex={-1}
              sx={{ flex: 1, minWidth: 0, px: { xs: 2, sm: 2.5, md: 5 }, py: { xs: 2.5, md: 4 }, outline: 'none', overflowWrap: 'break-word' }}>
              {loadingView ? (
                <ViewSkeleton />
              ) : (
                <Routes>
                  <Route path="/chamados" element={<TicketList tickets={tickets} role={session.role} user={session} onOpen={(id) => navigate(`/chamados/${id}`)} onNew={() => setShowNewTicket(true)} />} />
                  <Route path="/chamados/:id" element={<TicketDetailRoute tickets={tickets} session={session} onUpdate={updateTicket} />} />
                  <Route path="/relatorios" element={<Reports tickets={tickets} role={session.role} user={session} />} />
                  <Route path="/usuarios" element={<RoleRoute allow={['admin']} role={session.role}><UserAdmin /></RoleRoute>} />
                  <Route path="*" element={<Navigate to="/chamados" replace />} />
                </Routes>
              )}
            </Box>
          </Box>
          {showNewTicket && session.role === 'cliente' && <NewTicketModal user={session} onClose={() => setShowNewTicket(false)} onCreate={createTicket} />}
        </Box>
      )}
      {/* Notificações: sonner (toast.success / toast.error / toast.warning). */}
      <Toaster position="bottom-center" richColors closeButton theme={themeMode} toastOptions={{ style: { fontSize: 14 } }} />
      </LocalizationProvider>
    </ThemeProvider>
  );
}
