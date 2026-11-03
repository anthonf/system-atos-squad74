import { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { AppBar, Box, IconButton, Toolbar, Typography, Menu, MenuItem, Switch } from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import QueueIcon from '@mui/icons-material/Inbox';
import LogoutIcon from '@mui/icons-material/Logout';
import AccountCircle from '@mui/icons-material/AccountCircle';
import { useAuth } from '../auth/AuthContext';
import { mascararEmail } from '../utils/mask';

export function Layout({
  children,
  dark,
  onToggleDark,
}: {
  children: React.ReactNode;
  dark: boolean;
  onToggleDark: () => void;
}) {
  const { sessao, sair } = useAuth();
  const nav = useNavigate();
  const [anchor, setAnchor] = useState<null | HTMLElement>(null);

  function doSair() {
    sair();
    setAnchor(null);
    nav('/login');
  }

  return (
    <div className="layout">
      <a className="skip-link" href="#conteudo">
        Pular para o conteúdo
      </a>
      <aside className="sidebar" aria-label="Navegação principal">
        <Typography variant="subtitle1" fontWeight={700} sx={{ px: 1 }}>
          <span className="label">SupportAtos</span>
        </Typography>
        <nav>
          <NavLink to="/" className={({ isActive }) => (isActive ? 'active' : '')}>
            <DashboardIcon fontSize="small" /> <span className="label">Dashboard</span>
          </NavLink>
          <NavLink to="/fila" className={({ isActive }) => (isActive ? 'active' : '')}>
            <QueueIcon fontSize="small" /> <span className="label">Fila</span>
          </NavLink>
        </nav>
      </aside>
      <div className="main">
        <AppBar position="sticky" color="default" elevation={0} className="topbar" component="header">
          <Toolbar disableGutters sx={{ width: '100%', gap: 1 }}>
            <Box flex={1} />
            <Typography variant="body2" color="text.secondary" aria-label="Tema">
              {dark ? 'Escuro' : 'Claro'}
            </Typography>
            <Switch checked={dark} onChange={onToggleDark} aria-label="Alternar tema claro/escuro" />
            <IconButton aria-label="Perfil" onClick={(e) => setAnchor(e.currentTarget)}>
              <AccountCircle />
            </IconButton>
            <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}>
              <MenuItem disabled>
                {sessao?.nome} ({sessao?.papel})
                <br />
                {sessao?.email ? mascararEmail(sessao.email) : ''}
              </MenuItem>
              <MenuItem onClick={doSair}>
                <LogoutIcon fontSize="small" style={{ marginRight: 8 }} /> Sair
              </MenuItem>
            </Menu>
          </Toolbar>
        </AppBar>
        <main id="conteudo" className="content" tabIndex={-1}>
          {children}
        </main>
      </div>
    </div>
  );
}
