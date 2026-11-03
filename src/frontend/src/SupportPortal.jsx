// Aplicação: estado global (sessão, chamados, notificações), tema MUI e roteamento simples por view.
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Box, CssBaseline, Skeleton, Snackbar, Stack, ThemeProvider } from '@mui/material';
import { buildTheme } from './theme';
import LoginScreen from './components/Login';
import { Sidebar, TopBar } from './components/Layout';
import TicketList, { NewTicketModal } from './components/TicketList';
import TicketDetail from './components/TicketDetail';
import Reports from './components/Reports';
import * as api from './api';
import { mapChamadoFromApi, mapDetalheFromApi, mapSessaoFromApi, reverseStatus } from './mappers';

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
  const [tickets, setTickets] = useState([]);
  const [view, setView] = useState('lista');
  const [selectedId, setSelectedId] = useState(null);
  const [selectedDetail, setSelectedDetail] = useState(null);
  const [loadingView, setLoadingView] = useState(false);
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [toast, setToast] = useState(null);
  const [notifications, setNotifications] = useState([]);
  const [showNewTicket, setShowNewTicket] = useState(false);
  const [apiError, setApiError] = useState(null);
  const unreadCount = notifications.filter((n) => !n.lida).length;

  const notify = (msg) => setToast(msg);
  const markAllRead = () => setNotifications((prev) => prev.map((n) => ({ ...n, lida: true })));
  const markOneRead = (id) => setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)));

  // --- Carregar chamados da API ---
  const loadTickets = useCallback(async () => {
    if (!api.getToken()) return;
    try {
      setApiError(null);
      const data = await api.listarChamados();
      setTickets((data || []).map(mapChamadoFromApi));
    } catch (err) {
      setApiError(err.message);
      console.error('Erro ao carregar chamados:', err);
    }
  }, []);

  // Polling a cada 30s
  useEffect(() => {
    if (!session) return;
    loadTickets();
    const interval = setInterval(loadTickets, 30000);
    return () => clearInterval(interval);
  }, [session, loadTickets]);

  // --- Login ---
  const handleLogin = useCallback(async (tokenOrSession) => {
    // Login do cliente (simulado) — recebe objeto de sessão direto
    if (typeof tokenOrSession === 'object' && tokenOrSession.nome) {
      setSession(tokenOrSession);
      return;
    }
    // Login do agente via JWT
    const token = tokenOrSession;
    api.setToken(token);
    try {
      const sessaoDto = await api.getSessao();
      const sessao = mapSessaoFromApi(sessaoDto);
      setSession(sessao);
      notify(`Bem-vindo, ${sessao.nome}!`);
    } catch (err) {
      api.clearToken();
      throw err; // propagado para Login.jsx mostrar o erro
    }
  }, []);

  const handleLogout = useCallback(() => {
    api.clearToken();
    setSession(null);
    setTickets([]);
    setSelectedId(null);
    setSelectedDetail(null);
    setNotifications([]);
    setView('lista');
  }, []);

  // --- Navegação ---
  const changeView = (v) => {
    setMobileNavOpen(false);
    setLoadingView(true);
    setView(v);
    setSelectedId(null);
    setSelectedDetail(null);
    window.setTimeout(() => setLoadingView(false), 450);
  };

  const openTicket = async (id) => {
    setLoadingView(true);
    setSelectedId(id);
    setView('detalhe');
    try {
      const detalhe = await api.consultarChamado(id);
      setSelectedDetail(mapDetalheFromApi(detalhe));
    } catch (err) {
      notify(`Erro ao abrir chamado: ${err.message}`);
      // fallback para dados da lista
      const fallback = tickets.find((t) => t.id === id);
      setSelectedDetail(fallback || null);
    }
    setLoadingView(false);
  };

  // Recarrega o detalhe do ticket aberto
  const reloadDetail = async (codigoPublico) => {
    try {
      const detalhe = await api.consultarChamado(codigoPublico);
      const mapped = mapDetalheFromApi(detalhe);
      setSelectedDetail(mapped);
      // Atualiza também na lista
      setTickets((prev) => prev.map((t) => t.id === codigoPublico ? { ...t, ...mapped } : t));
    } catch (err) {
      notify(`Erro ao recarregar: ${err.message}`);
    }
  };

  // --- Ações nos chamados (integradas com API) ---
  const updateTicket = async (updated, action) => {
    const codigoPublico = updated.id;
    try {
      if (action === 'responder') {
        await api.responderChamado(codigoPublico, updated._reply, 0);
        notify('Resposta enviada.');
      } else if (action === 'nota') {
        await api.responderChamado(codigoPublico, updated._note, 1);
        notify('Nota interna adicionada.');
      } else if (action === 'status') {
        await api.mudarStatus(codigoPublico, reverseStatus(updated._novoStatus));
        notify(`Status alterado para ${updated._novoStatus}.`);
      } else if (action === 'assumir') {
        await api.atribuirAgente(codigoPublico, session.usuarioId);
        notify('Chamado assumido.');
      } else if (action === 'repassar') {
        await api.atribuirAgente(codigoPublico, updated._targetAgenteId);
        notify('Chamado repassado.');
      } else {
        // fallback genérico — atualiza local
        setTickets((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
        if (selectedDetail?.id === updated.id) setSelectedDetail(updated);
        return;
      }
      // Recarrega do backend após a ação
      await reloadDetail(codigoPublico);
      await loadTickets();
    } catch (err) {
      notify(`Erro: ${err.message}`);
    }
  };

  const createTicket = async (input) => {
    try {
      const created = await api.criarChamado({
        produto: input.produto,
        categoria: input.categoria,
        mensagemInicial: input.mensagemInicial || input.descricao || 'Novo chamado',
        titulo: input.titulo || undefined,
        descricao: input.descricao || undefined,
      });
      setShowNewTicket(false);
      notify(`Chamado ${created.codigoPublico} registrado.`);
      setNotifications((prev) => [
        { id: Date.now(), texto: `Chamado ${created.codigoPublico} registrado com sucesso.`, data: 'agora', lida: false },
        ...prev,
      ]);
      await loadTickets();
    } catch (err) {
      notify(`Erro ao criar chamado: ${err.message}`);
    }
  };

  // --- Render ---
  if (!session) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <LoginScreen
          themeMode={themeMode}
          onToggleTheme={() => setThemeMode((m) => (m === 'light' ? 'dark' : 'light'))}
          onLogin={handleLogin}
        />
      </ThemeProvider>
    );
  }

  const role = session.role || (session.papel === 'cliente' ? 'cliente' : 'agente');
  const renderView = () => {
    if (loadingView) return <ViewSkeleton />;
    if (view === 'detalhe' && selectedDetail) {
      return (
        <TicketDetail
          ticket={selectedDetail}
          role={role}
          user={session}
          onBack={() => changeView('lista')}
          onUpdate={updateTicket}
          notify={notify}
        />
      );
    }
    if (view === 'relatorios') return <Reports tickets={tickets} role={role} user={session} />;
    return (
      <TicketList
        tickets={tickets}
        role={role}
        user={session}
        onOpen={openTicket}
        onRefresh={loadTickets}
        showNewTicket={showNewTicket}
        onShowNewTicket={() => setShowNewTicket(true)}
        apiError={apiError}
      />
    );
  };

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden', bgcolor: 'background.default' }}>
        <Sidebar
          session={session}
          view={view}
          setView={changeView}
          onLogout={handleLogout}
          themeMode={themeMode}
          setThemeMode={setThemeMode}
          mobileOpen={mobileNavOpen}
          onMobileClose={() => setMobileNavOpen(false)}
        />
        <Box sx={{ flex: 1, minWidth: 0, height: '100%', overflowY: 'auto', display: 'flex', flexDirection: 'column' }}>
          <TopBar
            session={session}
            themeMode={themeMode}
            notifications={notifications}
            unreadCount={unreadCount}
            onOpenNav={() => setMobileNavOpen(true)}
            onMarkAllRead={markAllRead}
            onMarkOneRead={markOneRead}
          />
          <Box component="main" sx={{ flex: 1, px: { xs: 2.5, md: 5 }, py: { xs: 3, md: 4 } }}>
            {renderView()}
          </Box>
        </Box>
      </Box>
      {showNewTicket && (
        <NewTicketModal
          role={role}
          user={session}
          onClose={() => setShowNewTicket(false)}
          onCreate={createTicket}
        />
      )}
      <Snackbar open={!!toast} autoHideDuration={4000} onClose={() => setToast(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}>
        <Alert severity="info" variant="filled" onClose={() => setToast(null)}>{toast}</Alert>
      </Snackbar>
    </ThemeProvider>
  );
}
