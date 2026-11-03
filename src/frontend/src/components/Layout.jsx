// Casca da aplicação: menu lateral (Drawer) e barra superior (AppBar).
import React, { useState } from 'react';
import {
  AppBar, Avatar, Badge, Box, Breadcrumbs, Button, Divider, Drawer, IconButton, List, ListItemButton,
  ListItemIcon, ListItemText, Popover, Stack, Toolbar, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import {
  BarChartOutlined, Close, ConfirmationNumberOutlined, DarkModeOutlined, HeadsetMicOutlined, LightModeOutlined,
  Logout, Menu as MenuIcon, NotificationsNone, ShieldOutlined,
} from '@mui/icons-material';
import { BrandMark } from './ui';
import { MONO } from '../theme';

export const DRAWER_WIDTH = 256;

function SidebarContent({ session, view, setView, onLogout, themeMode, setThemeMode, onClose }) {
  const { atos } = useTheme();
  const nav = [
    { id: 'lista', label: session.role === 'agente' ? 'Fila de atendimento' : 'Meus chamados', icon: session.role === 'agente' ? HeadsetMicOutlined : ConfirmationNumberOutlined },
    { id: 'relatorios', label: 'Relatórios', icon: BarChartOutlined },
  ];
  const soft = { color: atos.menuText, opacity: 0.7 };
  return (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column', justifyContent: 'space-between', color: atos.menuText }}>
      <Box>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-start" sx={{ px: 2.5, pt: 3, pb: 2.5, borderBottom: '1px solid rgba(255,255,255,0.12)' }}>
          <Box>
            <BrandMark onDark width={150} />
            <Stack direction="row" alignItems="center" spacing={0.75} mt={1.5} sx={soft}>
              <ShieldOutlined sx={{ fontSize: 13 }} />
              <Typography variant="caption" sx={{ color: 'inherit', fontSize: 10 }}>Ambiente seguro</Typography>
            </Stack>
          </Box>
          {onClose && <IconButton aria-label="Fechar menu" onClick={onClose} sx={{ color: atos.menuText, mt: -0.5, mr: -1 }}><Close /></IconButton>}
        </Stack>
        <List sx={{ mt: 1.5, px: 1.5 }}>
          {nav.map((item) => {
            const Icon = item.icon;
            return (
              <ListItemButton
                key={item.id}
                selected={view === item.id}
                onClick={() => setView(item.id)}
                sx={{
                  borderRadius: 2, mb: 0.5, color: atos.menuText, opacity: 0.78,
                  '&.Mui-selected, &.Mui-selected:hover': { bgcolor: 'rgba(255,255,255,0.14)', opacity: 1 },
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.08)', opacity: 1 },
                }}
              >
                <ListItemIcon sx={{ minWidth: 36, color: 'inherit' }}><Icon fontSize="small" /></ListItemIcon>
                <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14, fontWeight: view === item.id ? 600 : 500 }} />
              </ListItemButton>
            );
          })}
        </List>
      </Box>

      <Box sx={{ px: 2.5, py: 2.5, borderTop: '1px solid rgba(255,255,255,0.12)' }}>
        <Button
          fullWidth variant="outlined" size="small"
          startIcon={themeMode === 'light' ? <DarkModeOutlined /> : <LightModeOutlined />}
          onClick={() => setThemeMode(themeMode === 'light' ? 'dark' : 'light')}
          sx={{ mb: 2, justifyContent: 'flex-start', color: atos.menuText, borderColor: 'rgba(255,255,255,0.2)', '&:hover': { borderColor: 'rgba(255,255,255,0.4)', color: atos.menuText } }}
        >
          {themeMode === 'light' ? 'Tema escuro' : 'Tema claro'}
        </Button>
        <Typography variant="caption" sx={{ ...soft, display: 'block', mb: 0.5 }}>
          {session.role === 'cliente' ? 'Sincronizado via SSO' : 'Equipe de suporte'}
        </Typography>
        <Typography sx={{ fontWeight: 600, fontSize: 14 }}>{session.nome}</Typography>
        <Typography variant="caption" noWrap sx={{ ...soft, display: 'block', mb: 2 }}>
          {session.role === 'cliente' ? session.empresa : session.email}
        </Typography>
        <Button fullWidth variant="outlined" startIcon={<Logout />} onClick={onLogout}
          sx={{ color: atos.menuText, borderColor: 'rgba(255,255,255,0.25)', '&:hover': { borderColor: 'rgba(255,255,255,0.5)', color: atos.menuText } }}>
          Sair
        </Button>
      </Box>
    </Box>
  );
}

export function Sidebar({ mobileOpen, onMobileClose, ...props }) {
  const { atos } = useTheme();
  const paper = { width: DRAWER_WIDTH, background: atos.menuGradient, border: 0, boxSizing: 'border-box' };
  return (
    <>
      <Drawer variant="temporary" open={mobileOpen} onClose={onMobileClose} ModalProps={{ keepMounted: true }}
        sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': paper }}>
        <SidebarContent {...props} onClose={onMobileClose} />
      </Drawer>
      <Drawer variant="permanent" open
        sx={{ display: { xs: 'none', md: 'block' }, width: DRAWER_WIDTH, flexShrink: 0, '& .MuiDrawer-paper': paper }}>
        <SidebarContent {...props} />
      </Drawer>
    </>
  );
}

export function TopBar({ session, notifications, unreadCount, onOpenNav, onMarkAllRead, onMarkOneRead, themeMode }) {
  const [anchor, setAnchor] = useState(null);
  const initials = session.nome.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0].toUpperCase()).join('');
  const context = session.role === 'cliente' ? session.empresa : 'Equipe de suporte';
  return (
    <AppBar position="sticky" color="inherit" elevation={0} sx={{ bgcolor: 'background.paper', borderBottom: 1, borderColor: 'divider' }}>
      <Toolbar sx={{ minHeight: 66, px: { xs: 2, md: 5 }, gap: 2 }}>
        <IconButton aria-label="Abrir menu" onClick={onOpenNav} sx={{ display: { md: 'none' }, ml: -1 }}><MenuIcon /></IconButton>
        <Box sx={{ display: { md: 'none' } }}><BrandMark onDark={themeMode === 'dark'} width={110} /></Box>

        <Breadcrumbs separator="•" sx={{ display: { xs: 'none', md: 'block' }, '& .MuiBreadcrumbs-separator': { color: 'primary.main', fontSize: 10 } }}>
          <Typography variant="caption">Central de atendimento</Typography>
          <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600 }}>{context}</Typography>
        </Breadcrumbs>

        <Stack direction="row" alignItems="center" spacing={1.5} sx={{ ml: 'auto' }}>
          <IconButton aria-label="Notificações" onClick={(e) => setAnchor(e.currentTarget)} sx={{ border: 1, borderColor: 'divider', width: 36, height: 36 }}>
            <Badge color="primary" variant="dot" invisible={unreadCount === 0}><NotificationsNone fontSize="small" /></Badge>
          </IconButton>
          <Avatar sx={{ width: 32, height: 32, fontSize: 11, fontWeight: 700, color: '#fff', background: (t) => `linear-gradient(145deg, ${t.atos.brandPrimaryStrong}, ${t.atos.brandPrimaryMuted})` }}>
            {initials}
          </Avatar>
          <Box sx={{ display: { xs: 'none', sm: 'block' } }}>
            <Typography sx={{ fontSize: 12, fontWeight: 600, lineHeight: 1.3 }}>{session.nome}</Typography>
            <Typography variant="caption" sx={{ fontSize: 10 }}>{session.role === 'cliente' ? 'Cliente Atos Capital' : 'Equipe de suporte'}</Typography>
          </Box>
        </Stack>

        <Popover open={Boolean(anchor)} anchorEl={anchor} onClose={() => setAnchor(null)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          PaperProps={{ sx: { width: 320, mt: 1 } }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ px: 2, py: 1.5 }}>
            <Typography variant="h3" sx={{ fontSize: 13 }}>Notificações</Typography>
            {unreadCount > 0 && <Button size="small" onClick={onMarkAllRead} sx={{ minHeight: 0, fontSize: 12 }}>Marcar tudo como lido</Button>}
          </Stack>
          <Divider />
          {notifications.length === 0 ? (
            <Typography sx={{ p: 3, textAlign: 'center', color: 'text.disabled' }}>Nenhuma notificação.</Typography>
          ) : (
            <List disablePadding sx={{ maxHeight: 340, overflow: 'auto' }}>
              {notifications.map((n) => (
                <ListItemButton key={n.id} onClick={() => onMarkOneRead(n.id)} divider sx={{ alignItems: 'flex-start', gap: 1.25, bgcolor: n.lida ? 'transparent' : 'action.hover' }}>
                  <Box sx={{ width: 7, height: 7, borderRadius: '50%', mt: 0.9, flexShrink: 0, bgcolor: n.lida ? 'transparent' : 'primary.main' }} />
                  <ListItemText
                    primary={n.texto}
                    secondary={n.data}
                    primaryTypographyProps={{ fontSize: 13, fontWeight: n.lida ? 400 : 600 }}
                    secondaryTypographyProps={{ fontFamily: MONO, fontSize: 11 }}
                  />
                </ListItemButton>
              ))}
            </List>
          )}
        </Popover>
      </Toolbar>
    </AppBar>
  );
}
