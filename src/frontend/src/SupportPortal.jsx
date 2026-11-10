// Aplicação: estado global (sessão, chamados, notificações), tema MUI e roteamento simples por view.
import React, { useMemo, useState } from 'react';
import { Alert, Box, CssBaseline, Skeleton, Snackbar, Stack, ThemeProvider } from '@mui/material';
import { buildTheme } from './theme';
import { seed, seedNotifications } from './data';
import LoginScreen from './components/Login';
import { Sidebar, TopBar } from './components/Layout';
import TicketList, { NewTicketModal } from './components/TicketList';
import TicketDetail from './components/TicketDetail';
import Reports from './components/Reports';

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
  const [themeMode, setThemeMode] = useState('light');
  const theme = useMemo(() => buildTheme(themeMode), [themeMode]);

  const [session, setSession] = useState(null);
  const [tickets, setTickets] = useState(seed);
  const [view, setView] = useState('lista');
  const [selectedId, setSelectedId] = useState(null);
  const [loadingView, setLoadingView] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [toast, setToast] = useState(null);
  const [notifications, setNotifications] = useState(seedNotifications);
  const [showNewTicket, setShowNewTicket] = useState(false);
  const unreadCount = notifications.filter((n) => !n.lida).length;

  const notify = (msg) => setToast(msg);
  const markAllRead = () => setNotifications((prev) => prev.map((n) => ({ ...n, lida: true })));
  const markOneRead = (id) => setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)));

  const changeView = (v) => {
    setMobileNavOpen(false);
    setLoadingView(true);
    setView(v);
    setSelectedId(null);
    window.setTimeout(() => setLoadingView(false), 450);
  };
  const openTicket = (id) => {
    setLoadingView(true);
    setSelectedId(id);
    setView('detalhe');
    window.setTimeout(() => setLoadingView(false), 400);
  };
  const updateTicket = (updated) => setTickets((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
  const createTicket = (t) => {
    setTickets((prev) => [t, ...prev]);
    setNotifications((prev) => [{ id: Date.now(), texto: `Chamado ${t.id} registrado com sucesso.`, data: 'agora', lida: false }, ...prev]);
    setShowNewTicket(false);
    notify(`Chamado ${t.id} registrado.`);
  };
  const logout = () => { setSession(null); setView('lista'); setSelectedId(null); setShowNewTicket(false); };

  const role = session?.role;
  const selected = tickets.find((t) => t.id === selectedId);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {!session ? (
        <LoginScreen onLogin={setSession} themeMode={themeMode} setThemeMode={setThemeMode} />
      ) : (
        <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden', bgcolor: 'background.default' }}>
          <Sidebar
            session={session} view={view} setView={changeView} onLogout={logout}
            themeMode={themeMode} setThemeMode={setThemeMode}
            mobileOpen={mobileNavOpen} onMobileClose={() => setMobileNavOpen(false)}
          />
          <Box sx={{ flex: 1, minWidth: 0, height: '100%', overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
            <TopBar
              session={session} themeMode={themeMode} notifications={notifications} unreadCount={unreadCount}
              onOpenNav={() => setMobileNavOpen(true)} onMarkAllRead={markAllRead} onMarkOneRead={markOneRead}
            />
            <Box component="main" sx={{ flex: 1, px: { xs: 2.5, md: 5 }, py: { xs: 3, md: 4 } }}>
              {loadingView ? (
                <ViewSkeleton />
              ) : (
                <>
                  {view === 'lista' && <TicketList tickets={tickets} role={role} user={session} onOpen={openTicket} onNew={() => setShowNewTicket(true)} />}
                  {view === 'relatorios' && <Reports tickets={tickets} role={role} user={session} />}
                  {view === 'detalhe' && selected && (
                    <TicketDetail ticket={selected} role={role} user={session} onBack={() => changeView('lista')} onUpdate={updateTicket} notify={notify} />
                  )}
                </>
              )}
            </Box>
          </Box>
          {showNewTicket && role === 'cliente' && <NewTicketModal user={session} onClose={() => setShowNewTicket(false)} onCreate={createTicket} />}
        </Box>
      )}
      <Snackbar open={Boolean(toast)} autoHideDuration={3000} onClose={() => setToast(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}>
        <Alert severity="success" variant="filled" onClose={() => setToast(null)} sx={{ alignItems: 'center' }}>{toast}</Alert>
      </Snackbar>
    </ThemeProvider>
  );
}
