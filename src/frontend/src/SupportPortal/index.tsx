import { useApp, useTicketDetailRoute, type TicketDetailRouteProps } from './useApp';
// Aplicação: estado global (sessão, chamados, notificações), tema MUI e rotas (React Router) com navegação por papel.
import { Alert, Box, Button, CssBaseline, ThemeProvider } from '@mui/material';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { enUS, esES, ptBR } from '@mui/x-date-pickers/locales';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import React from 'react';
import { useTranslation } from 'react-i18next';
import { Navigate, Route, Routes } from 'react-router-dom';
import { Toaster } from 'sonner';
import Activate from '../components/Activate';
import { Sidebar, TopBar } from '../components/Layout';
import LoginScreen from '../components/Login';
import Reports from '../components/Reports';
import TicketDetail from '../components/TicketDetail';
import TicketList, { NewTicketModal } from '../components/TicketList';
import { ViewSkeleton, visuallyHidden } from '../components/ui';
import UserAdmin from '../components/UserAdmin';
import { DAYJS_LOCALE } from '../i18n';
import type { Role } from '../types';

// Textos dos seletores de data (x-date-pickers) por idioma da interface.
const PICKER_TEXT = {
  pt: ptBR.components.MuiLocalizationProvider.defaultProps.localeText,
  en: enUS.components.MuiLocalizationProvider.defaultProps.localeText,
  es: esES.components.MuiLocalizationProvider.defaultProps.localeText,
};

/** /chamados/:id — cliente só abre chamado da própria empresa; id inexistente volta para a lista. */
function TicketDetailRoute({ tickets, session, onUpdate }: TicketDetailRouteProps) {
  const { t: tr } = useTranslation();
  const { ticket, canView, loading, error, recarregar, backToList } = useTicketDetailRoute({ tickets, session, onUpdate });
  if (!ticket || !canView) return loading ? <ViewSkeleton /> : <Navigate to="/chamados" replace />;
  if (loading) return <ViewSkeleton />;
  return (
    <>
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} action={<Button color="inherit" size="small" onClick={() => void recarregar()}>{tr('common.tryAgain')}</Button>}>
          {tr('detail.loadError')}
        </Alert>
      )}
      <TicketDetail ticket={ticket} role={session.role} user={session} onBack={backToList} onUpdate={onUpdate} />
    </>
  );
}

/** Rota restrita a alguns papéis; os demais voltam para a lista de chamados. */
function RoleRoute({ allow, role, children }: { allow: Role[]; role: Role; children: React.ReactElement }) {
  return allow.includes(role) ? children : <Navigate to="/chamados" replace />;
}

export default function SupportPortal() {
  const {
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
  } = useApp();

  return (
    <ThemeProvider theme={theme}>
      <LocalizationProvider dateAdapter={AdapterDayjs} adapterLocale={DAYJS_LOCALE[lang]} localeText={PICKER_TEXT[lang]}>
        <CssBaseline />
        {location.pathname === '/ativar' ? (
          <Routes>
            <Route path="/ativar" element={<Activate />} />
          </Routes>
        ) : authStatus === 'restoring' ? (
          <Box role="status" aria-label={tr('common.loading')} sx={{ p: 4 }}><ViewSkeleton /></Box>
        ) : !session ? (
          <Routes>
            <Route path="/login" element={<LoginScreen onLogin={login} themeMode={themeMode} setThemeMode={setThemeMode}
              activationSuccess={activationSuccess} />} />
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
                <Routes>
                  <Route path="/chamados" element={<TicketList tickets={tickets} loading={ticketsLoading} error={ticketsError} onRefresh={carregarChamados} role={session.role} user={session} onOpen={(id) => navigate(`/chamados/${id}`)} onNew={() => setShowNewTicket(true)} />} />
                  <Route path="/chamados/:id" element={<TicketDetailRoute tickets={tickets} session={session} onUpdate={updateTicket} />} />
                  <Route path="/relatorios" element={<Reports tickets={tickets} role={session.role} user={session} />} />
                  <Route path="/usuarios" element={<RoleRoute allow={['admin']} role={session.role}><UserAdmin /></RoleRoute>} />
                  <Route path="*" element={<Navigate to="/chamados" replace />} />
                </Routes>
              </Box>
            </Box>
            {showNewTicket && (session.role === 'cliente' || session.role === 'admin') && <NewTicketModal user={session} onClose={() => setShowNewTicket(false)} onCreate={createTicket} />}
          </Box>
        )}
        {/* Notificações: sonner (toast.success / toast.error / toast.warning). */}
        <Toaster position="bottom-center" richColors closeButton theme={themeMode} toastOptions={{ style: { fontSize: 14 } }} />
      </LocalizationProvider>
    </ThemeProvider>
  );
}
