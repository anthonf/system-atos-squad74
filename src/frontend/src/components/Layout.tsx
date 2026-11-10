// Casca da aplicação: menu lateral (Drawer) e barra superior (AppBar).
import React, { useState } from 'react';
import {
  AppBar, Avatar, Badge, Box, Breadcrumbs, Button, Divider, Drawer, IconButton, List, ListItemButton,
  ListItemIcon, ListItemText, Popover, Stack, Toolbar, Typography,
} from '@mui/material';
import { NavLink } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import { useTranslation } from 'react-i18next';
import type { SvgIconComponent } from '@mui/icons-material';
import {
  BarChartOutlined, Close, ConfirmationNumberOutlined, DarkModeOutlined, HeadsetMicOutlined, LightModeOutlined,
  Logout, ManageAccountsOutlined, Menu as MenuIcon, NotificationsNone, ShieldOutlined,
} from '@mui/icons-material';
import { BrandMark, visuallyHidden } from './ui';
import LanguageSwitcher from './LanguageSwitcher';
import { MONO } from '../theme';
import type { AppNotification, Session, ThemeMode } from '../types';

export const DRAWER_WIDTH = 256;

interface SidebarContentProps {
  session: Session;
  onLogout: () => void;
  themeMode: ThemeMode;
  setThemeMode: (mode: ThemeMode) => void;
  onClose?: () => void;
}

interface NavItem { path: string; label: string; icon: SvgIconComponent }

function SidebarContent({ session, onLogout, themeMode, setThemeMode, onClose }: SidebarContentProps) {
  const { atos } = useTheme();
  const { t } = useTranslation();
  // Equipe e administrador veem a fila; o administrador também tem o cadastro de usuários.
  const isClient = session.role === 'cliente';
  const nav: NavItem[] = [
    { path: '/chamados', label: isClient ? t('nav.myTickets') : t('nav.queue'), icon: isClient ? ConfirmationNumberOutlined : HeadsetMicOutlined },
    { path: '/relatorios', label: t('nav.reports'), icon: BarChartOutlined },
    ...(session.role === 'admin' ? [{ path: '/usuarios' as const, label: t('nav.users'), icon: ManageAccountsOutlined }] : []),
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
              <Typography variant="caption" sx={{ color: 'inherit', fontSize: 12 }}>{t('nav.secure')}</Typography>
            </Stack>
          </Box>
          {onClose && <IconButton aria-label={t('nav.closeMenu')} onClick={onClose} sx={{ color: atos.menuText, mt: -0.5, mr: -1 }}><Close /></IconButton>}
        </Stack>
        <List component="nav" aria-label={t('a11y.mainMenu')} sx={{ mt: 1.5, px: 1.5 }}>
          {nav.map((item) => {
            const Icon = item.icon;
            return (
              <ListItemButton
                key={item.path}
                component={NavLink}
                to={item.path}
                onClick={onClose}
                sx={{
                  borderRadius: 2, mb: 0.5, color: atos.menuText, opacity: 0.78,
                  '&.active, &.active:hover': { bgcolor: 'rgba(255,255,255,0.14)', opacity: 1 },
                  '&:hover': { bgcolor: 'rgba(255,255,255,0.08)', opacity: 1 },
                }}
              >
                <ListItemIcon sx={{ minWidth: 36, color: 'inherit' }}><Icon fontSize="small" /></ListItemIcon>
                <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14, fontWeight: 500 }} />
              </ListItemButton>
            );
          })}
        </List>
      </Box>

      <Box sx={{ px: 2.5, py: 2.5, borderTop: '1px solid rgba(255,255,255,0.12)' }}>
        <Box sx={{ mb: 1.5 }}><LanguageSwitcher onDark /></Box>
        <Button
          fullWidth variant="outlined" size="small"
          startIcon={themeMode === 'light' ? <DarkModeOutlined /> : <LightModeOutlined />}
          onClick={() => setThemeMode(themeMode === 'light' ? 'dark' : 'light')}
          sx={{ mb: 2, justifyContent: 'flex-start', color: atos.menuText, borderColor: 'rgba(255,255,255,0.2)', '&:hover': { borderColor: 'rgba(255,255,255,0.4)', color: atos.menuText } }}
        >
          {themeMode === 'light' ? t('nav.themeDark') : t('nav.themeLight')}
        </Button>
        <Typography variant="caption" sx={{ ...soft, display: 'block', mb: 0.5 }}>
          {t(`nav.captions.${session.role}`)}
        </Typography>
        <Typography sx={{ fontWeight: 600, fontSize: 14 }}>{session.nome}</Typography>
        <Typography variant="caption" noWrap sx={{ ...soft, display: 'block', mb: 2 }}>
          {session.role === 'cliente' ? session.empresa : session.email}
        </Typography>
        <Button fullWidth variant="outlined" startIcon={<Logout />} onClick={onLogout}
          sx={{ color: atos.menuText, borderColor: 'rgba(255,255,255,0.25)', '&:hover': { borderColor: 'rgba(255,255,255,0.5)', color: atos.menuText } }}>
          {t('nav.logout')}
        </Button>
      </Box>
    </Box>
  );
}

type SidebarProps = Omit<SidebarContentProps, 'onClose'> & { mobileOpen: boolean; onMobileClose: () => void };

export function Sidebar({ mobileOpen, onMobileClose, ...props }: SidebarProps) {
  const { atos } = useTheme();
  const paper = { width: DRAWER_WIDTH, maxWidth: '88vw', background: atos.menuGradient, border: 0, boxSizing: 'border-box', '& *:focus-visible': { outlineColor: '#ffffff' } };
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

interface TopBarProps {
  session: Session;
  notifications: AppNotification[];
  unreadCount: number;
  onOpenNav: () => void;
  onMarkAllRead: () => void;
  onMarkOneRead: (id: number) => void;
  themeMode: ThemeMode;
}

export function TopBar({ session, notifications, unreadCount, onOpenNav, onMarkAllRead, onMarkOneRead, themeMode }: TopBarProps) {
  const { t } = useTranslation();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const initials = session.nome.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0].toUpperCase()).join('');
  const context = session.role === 'cliente' ? session.empresa : t(`nav.context.${session.role}`);
  return (
    <AppBar position="sticky" color="inherit" elevation={0} sx={{ bgcolor: 'background.paper', borderBottom: 1, borderColor: 'divider' }}>
      <Toolbar sx={{ minHeight: 66, px: { xs: 2, md: 5 }, gap: 2 }}>
        <IconButton aria-label={t('nav.openMenu')} onClick={onOpenNav} sx={{ display: { md: 'none' }, ml: -1 }}><MenuIcon /></IconButton>
        <Box sx={{ display: { md: 'none' } }}><BrandMark onDark={themeMode === 'dark'} width={110} /></Box>

        <Breadcrumbs separator="•" sx={{ display: { xs: 'none', md: 'block' }, '& .MuiBreadcrumbs-separator': { color: 'primary.main', fontSize: 10 } }}>
          <Typography variant="caption">{t('nav.breadcrumbRoot')}</Typography>
          <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600 }}>{context}</Typography>
        </Breadcrumbs>

        <Stack direction="row" alignItems="center" spacing={1.5} sx={{ ml: 'auto' }}>
          <IconButton aria-label={unreadCount > 0 ? t('a11y.notificationsUnread', { n: unreadCount }) : t('notifications.title')}
            aria-haspopup="dialog" aria-expanded={Boolean(anchor)} onClick={(e) => setAnchor(e.currentTarget)}
            sx={{ border: 1, borderColor: 'divider', width: { xs: 44, md: 36 }, height: { xs: 44, md: 36 } }}>
            <Badge color="primary" variant="dot" invisible={unreadCount === 0}><NotificationsNone fontSize="small" /></Badge>
          </IconButton>
          <Avatar aria-hidden sx={{ width: 32, height: 32, fontSize: 11, fontWeight: 700, color: '#fff', background: (t) => `linear-gradient(145deg, ${t.atos.brandPrimaryStrong}, ${t.atos.brandPrimaryMuted})` }}>
            {initials}
          </Avatar>
          <Box sx={{ display: { xs: 'none', sm: 'block' } }}>
            <Typography sx={{ fontSize: 12, fontWeight: 600, lineHeight: 1.3 }}>{session.nome}</Typography>
            <Typography variant="caption" sx={{ fontSize: 12 }}>{t(`nav.roleLabels.${session.role}`)}</Typography>
          </Box>
        </Stack>

        <Popover open={Boolean(anchor)} anchorEl={anchor} onClose={() => setAnchor(null)}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}
          PaperProps={{ sx: { width: 'min(320px, calc(100vw - 24px))', mt: 1 } }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ px: 2, py: 1.5 }}>
            <Typography variant="h3" sx={{ fontSize: 13 }}>{t('notifications.title')}</Typography>
            {unreadCount > 0 && <Button size="small" onClick={onMarkAllRead} sx={{ minHeight: 0, fontSize: 12 }}>{t('notifications.markAll')}</Button>}
          </Stack>
          <Divider />
          {notifications.length === 0 ? (
            <Typography sx={{ p: 3, textAlign: 'center', color: 'text.disabled' }}>{t('notifications.empty')}</Typography>
          ) : (
            <List disablePadding sx={{ maxHeight: 340, overflow: 'auto' }}>
              {notifications.map((n) => (
                <ListItemButton key={n.id} onClick={() => onMarkOneRead(n.id)} divider sx={{ alignItems: 'flex-start', gap: 1.25, bgcolor: n.lida ? 'transparent' : 'action.hover' }}>
                  <Box aria-hidden sx={{ width: 7, height: 7, borderRadius: '50%', mt: 0.9, flexShrink: 0, bgcolor: n.lida ? 'transparent' : 'primary.main' }} />
                  {!n.lida && <Box component="span" sx={visuallyHidden}>{t('a11y.unread')}: </Box>}
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
