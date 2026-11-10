// Tela de login: vitrine do produto à esquerda, cartão de acesso à direita.
import React, { useState } from 'react';
import {
  Box, Button, Checkbox, CircularProgress, FormControlLabel, IconButton, InputAdornment, Link, Paper, Stack, Tab, Tabs,
  TextField, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  ArrowForward, DarkModeOutlined, LightModeOutlined, LockOutlined, MailOutline, ShieldOutlined, Visibility, VisibilityOff,
} from '@mui/icons-material';
import { BrandMark, Eyebrow } from './ui';
import { toast } from 'sonner';
import { loginDev } from '../api';
import FormTextField from './form/FormTextField';
import LanguageSwitcher from './LanguageSwitcher';
import { MONO } from '../theme';
import type { Role, Session, StatusKey, ThemeMode } from '../types';

const NOISE = "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 180 180' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.95' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='.18'/%3E%3C/svg%3E\")";
const PINK = '#f4a3b1';

function ProductPreview() {
  const { t } = useTranslation();
  const rows: { id: string; status: StatusKey; c: [string, string] }[] = [
    { id: '#00214', status: 'em_andamento', c: ['rgba(255,152,0,0.2)', '#ffa726'] },
    { id: '#00212', status: 'aguardando_cliente', c: ['rgba(186,104,200,0.2)', '#ba68c8'] },
    { id: '#00211', status: 'resolvido', c: ['rgba(102,187,106,0.2)', '#66bb6a'] },
  ];
  const stats = [[t('login.showcase.preview.open'), '6', '#f2eef0'], [t('login.showcase.preview.nearSla'), '3', '#ef5350'], [t('login.showcase.preview.resolved'), '12', '#66bb6a']];
  return (
    <Box aria-hidden sx={{ width: 'min(100%, 545px)', overflow: 'hidden', border: '1px solid rgba(255,255,255,0.12)', borderRadius: 3, bgcolor: '#101012', color: '#f2eef0', boxShadow: '0 32px 60px rgba(14,2,7,0.4)', transform: 'perspective(1200px) rotateY(2deg) rotateX(1deg)' }}>
      <Stack direction="row" alignItems="center" spacing={1.5} sx={{ height: 34, px: 1.75, bgcolor: '#1a191b', color: '#7d7880', fontSize: 7 }}>
        <Stack direction="row" spacing={0.6}>{[0, 1, 2].map((i) => <Box key={i} sx={{ width: 5, height: 5, borderRadius: '50%', bgcolor: '#454146' }} />)}</Stack>
        <Box sx={{ flex: 1, textAlign: 'center', fontSize: 7 }}>app.atoscapital.com/suporte</Box>
        <Box sx={{ width: 30 }} />
      </Stack>
      <Box sx={{ display: 'grid', gridTemplateColumns: '42px 1fr', height: 225 }}>
        <Stack alignItems="center" spacing={2} sx={{ pt: 1.4, borderRight: '1px solid #242126', bgcolor: '#121113' }}>
          <Box sx={{ width: 20, height: 20, display: 'grid', placeItems: 'center', borderRadius: 1, bgcolor: '#c24673', fontSize: 8, fontWeight: 800, color: '#fff' }}>A</Box>
          {[17, 12, 12, 12].map((w, i) => <Box key={i} sx={{ width: w, height: 3, borderRadius: 1, bgcolor: i === 0 ? '#c24673' : '#3a363c' }} />)}
        </Stack>
        <Box sx={{ p: '15px 18px', minWidth: 0 }}>
          <Typography sx={{ fontSize: 6, color: '#7d7880', textTransform: 'uppercase', letterSpacing: 0.7 }}>{t('login.showcase.preview.overview')}</Typography>
          <Typography sx={{ fontSize: 11, fontWeight: 700 }}>{t('login.showcase.preview.queue')}</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 1, my: 1.5 }}>
            {stats.map(([l, v, c]) => (
              <Box key={l} sx={{ p: '8px 10px', border: '1px solid #29262b', borderRadius: 1.5, bgcolor: '#171619' }}>
                <Typography sx={{ fontSize: 6, color: '#8a858c' }}>{l}</Typography>
                <Typography sx={{ fontSize: 11, fontWeight: 700, color: c, mt: 0.4 }}>{v}</Typography>
              </Box>
            ))}
          </Box>
          {rows.map((r) => (
            <Stack key={r.id} direction="row" alignItems="center" spacing={1} sx={{ p: '6px 8px', border: '1px solid #29262b', borderRadius: 1.5, bgcolor: '#171619', mb: 0.6 }}>
              <Box sx={{ fontFamily: MONO, fontSize: 6, color: '#8a858c', width: 38 }}>{r.id}</Box>
              <Box sx={{ flex: 1, height: 4, borderRadius: 1, bgcolor: '#2b282d' }} />
              <Box sx={{ fontSize: 6, fontWeight: 600, px: 0.75, py: 0.25, borderRadius: 9, bgcolor: r.c[0], color: r.c[1] }}>{t(`status.${r.status}`)}</Box>
            </Stack>
          ))}
        </Box>
      </Box>
    </Box>
  );
}

function Showcase() {
  const { atos } = useTheme();
  const { t } = useTranslation();
  return (
    <Box sx={{
      display: { xs: 'none', md: 'flex' }, flexDirection: 'column', position: 'relative', overflow: 'hidden', color: '#fff',
      px: 'clamp(42px, 5vw, 82px)', pt: 4.25, pb: 3.25,
      background: `linear-gradient(160deg, rgba(20,4,8,0.28), rgba(20,4,8,0.62)), linear-gradient(160deg, ${atos.brandPrimaryStrong}, ${atos.brandPrimaryMuted})`,
      '&::before': { content: '""', position: 'absolute', inset: 0, pointerEvents: 'none', opacity: 0.18, backgroundImage: NOISE },
      '& > *': { position: 'relative', zIndex: 1 },
    }}>
      <Box sx={{ position: 'absolute !important', zIndex: '0 !important', width: 420, height: 420, top: -240, right: -170, borderRadius: '50%', bgcolor: 'rgba(255,255,255,0.07)' }} />
      <Box sx={{ position: 'absolute !important', zIndex: '0 !important', width: 330, height: 330, left: -200, bottom: -140, borderRadius: '50%', border: '1px solid rgba(255,255,255,0.08)' }} />

      <Stack direction="row" alignItems="center" justifyContent="space-between">
        <BrandMark onDark width={150} />
        <Stack direction="row" alignItems="center" spacing={0.9} sx={{ color: 'rgba(255,255,255,0.65)' }}>
          <ShieldOutlined sx={{ fontSize: 14 }} /><Typography variant="caption" sx={{ color: 'inherit' }}>{t('login.showcase.secure')}</Typography>
        </Stack>
      </Stack>

      <Box sx={{ width: 'min(100%, 620px)', my: 'auto', py: { xs: 4, lg: 6 } }}>
        <Eyebrow color={PINK} sx={{ letterSpacing: '0.16em' }}>{t('login.showcase.eyebrow')}</Eyebrow>
        <Typography component="p" sx={{ fontSize: 'clamp(34px, 3.2vw, 52px)', fontWeight: 700, lineHeight: 1.1, letterSpacing: '-1.8px', maxWidth: 590 }}>
          {t('login.showcase.headlineA')}<br />{t('login.showcase.headlineB')}{' '}
          <Box component="em" sx={{ fontStyle: 'normal', color: PINK, position: 'relative', '&::after': { content: '""', position: 'absolute', left: 1, right: 0, bottom: -6, height: 5, borderTop: `2px solid ${PINK}`, borderRadius: '50%', transform: 'rotate(-1deg)' } }}>
            {t('login.showcase.headlineC')}
          </Box>
        </Typography>
        <Typography sx={{ maxWidth: 525, mt: 2.5, mb: 3.5, color: 'rgba(255,255,255,0.7)', lineHeight: 1.65 }}>
          {t('login.showcase.body')}
        </Typography>
        <ProductPreview />
        <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 3 }}>
          <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.8)', mr: 0.5 }}>{t('login.showcase.integratedWith')}</Typography>
          {['Joe SFA B1', 'SAP Business One', 'SAP S/4HANA'].map((s) => (
            <Box key={s} sx={{ fontSize: 11, px: 1.25, py: 0.6, borderRadius: 9, border: '1px solid rgba(255,255,255,0.3)', bgcolor: 'rgba(255,255,255,0.06)', color: 'rgba(255,255,255,0.9)' }}>{s}</Box>
          ))}
        </Stack>
      </Box>

      <Stack direction="row" spacing={2.75} sx={{ color: 'rgba(255,255,255,0.75)', fontSize: 12 }}>
        <span>{t('login.showcase.copyright')}</span><span>{t('login.showcase.privacy')}</span><span>{t('login.showcase.security')}</span>
      </Stack>
    </Box>
  );
}

interface LoginScreenProps {
  onLogin: (session: Session) => void;
  themeMode: ThemeMode;
  setThemeMode: (mode: ThemeMode) => void;
}

const PASSWORD_MIN = 4;

const teamSchema = yup.object({
  email: yup.string().trim().required('validation.required').email('validation.email'),
  password: yup.string().required('validation.required').min(PASSWORD_MIN, 'validation.passwordMin'),
});
type TeamForm = yup.InferType<typeof teamSchema>;

export default function LoginScreen({ onLogin, themeMode, setThemeMode }: LoginScreenProps) {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Role>('cliente');
  const [loading, setLoading] = useState(false);
  const [showSenha, setShowSenha] = useState(false);
  const isClient = tab === 'cliente';

  const { control, handleSubmit, reset } = useForm<TeamForm>({
    resolver: yupResolver(teamSchema),
    mode: 'onTouched',
    defaultValues: { email: '', password: '' },
  });

  // Em desenvolvimento (npm run dev) pega um JWT de dev na API para que as telas que usam a API funcionem.
  // Se a API estiver fora do ar o login segue (telas mockadas), com um aviso.
  const getDevToken = async (input: Parameters<typeof loginDev>[0]) => {
    if (!import.meta.env.DEV) return;
    try { await loginDev(input); } catch { toast.warning(t('login.devTokenFailed')); }
  };

  const handleSso = async () => {
    setLoading(true);
    await getDevToken({ email: 'fernanda.duarte@bancoconfianca.com', nome: 'Fernanda Duarte', papel: 'Cliente', grupoId: 'banco-confianca', grupoNome: 'Banco Confiança' });
    onLogin({ role: 'cliente', nome: 'Fernanda Duarte', empresa: 'Banco Confiança' });
  };

  const handleTeamSubmit = async ({ email }: TeamForm) => {
    setLoading(true);
    const nome = email.split('@')[0].split('.').map((p: string) => p.charAt(0).toUpperCase() + p.slice(1)).join(' ');
    await getDevToken({ email, nome, papel: tab === 'admin' ? 'Admin' : 'Agente' });
    onLogin({ role: tab, nome, empresa: null, email });
  };

  const spinner = <CircularProgress size={16} color="inherit" />;

  return (
    <Box sx={{ minHeight: '100vh', '@supports (min-height: 100dvh)': { minHeight: '100dvh' }, display: 'grid', gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1fr) minmax(0, 1fr)' }, bgcolor: 'background.default' }}>
      <Showcase />

      <Box component="main" sx={{ position: 'relative', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', px: { xs: 1.5, sm: 5 }, pt: { xs: 12, sm: 10 }, pb: 4.5, minHeight: '100vh', '@supports (min-height: 100dvh)': { minHeight: '100dvh' } }}>
        {/* Barra superior em fluxo (flex): marca, idioma e tema nunca se sobrepõem, mesmo em 320 px. */}
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1} flexWrap="wrap" useFlexGap
          sx={{ position: 'absolute', top: 16, left: 16, right: 16 }}>
          <Box sx={{ display: { md: 'none' } }}><BrandMark onDark={themeMode === 'dark'} width={110} /></Box>
          <Stack direction="row" alignItems="center" spacing={1} sx={{ ml: 'auto' }}>
            <LanguageSwitcher />
            <Button size="small" variant="outlined" onClick={() => setThemeMode(themeMode === 'light' ? 'dark' : 'light')}
              startIcon={themeMode === 'light' ? <DarkModeOutlined /> : <LightModeOutlined />}
              aria-label={t('login.themeToggleAria')}
              sx={{ borderRadius: 9, minHeight: 32, fontSize: 12, bgcolor: 'background.paper' }}>
              {themeMode === 'light' ? t('login.themeDark') : t('login.themeLight')}
            </Button>
          </Stack>
        </Stack>

        <Paper variant="outlined" sx={{ width: 'min(100%, 440px)', p: { xs: 3, sm: 4 }, borderRadius: 4, boxShadow: '0 30px 70px rgba(0,0,0,0.10)' }}>
          <Tabs value={tab} onChange={(_, v: Role) => { setTab(v); reset(); }} variant="fullWidth"
            sx={{ mb: 3, borderBottom: 1, borderColor: 'divider', '& .MuiTab-root': { minWidth: 0, px: { xs: 0.5, sm: 2 }, fontSize: { xs: 12.5, sm: 14 } } }}>
            <Tab value="cliente" label={t('login.tabs.cliente')} />
            <Tab value="agente" label={t('login.tabs.agente')} />
            <Tab value="admin" label={t('login.tabs.admin')} />
          </Tabs>

          <Stack direction="row" spacing={2} alignItems="flex-start" mb={3}>
            <Box sx={{ flex: '0 0 auto', width: 42, height: 42, display: 'grid', placeItems: 'center', borderRadius: 3, bgcolor: 'primary.main', color: 'primary.contrastText' }}>
              <ShieldOutlined />
            </Box>
            <Box>
              <Eyebrow>{t(`login.copy.${tab}.eyebrow`)}</Eyebrow>
              <Typography component="h1" sx={{ fontSize: 26, fontWeight: 700, lineHeight: 1.2, letterSpacing: '-0.6px' }}>{t('login.welcome')}</Typography>
              <Typography variant="body2" color="text.secondary" mt={0.5}>
                {t(`login.copy.${tab}.subtitle`)}
              </Typography>
            </Box>
          </Stack>

          {isClient ? (
            <Box>
              <Button fullWidth variant="contained" size="large" onClick={handleSso} disabled={loading}
                startIcon={loading ? spinner : <ShieldOutlined />} endIcon={!loading && <ArrowForward />} sx={{ minHeight: 46 }}>
                {loading ? t('login.sso.redirecting') : t('login.sso.button')}
              </Button>
              <Typography variant="caption" component="p" sx={{ mt: 2.25, textAlign: 'center', lineHeight: 1.5 }}>
                {t('login.sso.note')}
              </Typography>
              <Typography variant="caption" component="p" sx={{ mt: 1.25, textAlign: 'center' }}>
                {t('login.sso.noAccess')} <Link component="button" type="button" underline="hover" sx={{ fontSize: 11, fontWeight: 600, verticalAlign: 'baseline' }}>{t('login.sso.contactAdmin')}</Link>
              </Typography>
            </Box>
          ) : (
            <Box component="form" onSubmit={handleSubmit(handleTeamSubmit)} noValidate>
              <Stack spacing={2}>
                <FormTextField<TeamForm> name="email" control={control} required fullWidth type="email" autoComplete="username"
                  label={t('login.form.email')} placeholder={t('login.form.emailPlaceholder')}
                  InputProps={{ startAdornment: <InputAdornment position="start"><MailOutline fontSize="small" /></InputAdornment> }} />
                <FormTextField<TeamForm> name="password" control={control} required fullWidth autoComplete="current-password"
                  type={showSenha ? 'text' : 'password'} label={t('login.form.password')} errorParams={{ min: PASSWORD_MIN }}
                  InputProps={{
                    startAdornment: <InputAdornment position="start"><LockOutlined fontSize="small" /></InputAdornment>,
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton size="small" aria-label={showSenha ? t('login.form.hidePassword') : t('login.form.showPassword')} onClick={() => setShowSenha((v) => !v)}>
                          {showSenha ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }} />
              </Stack>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mt={1}>
                <FormControlLabel control={<Checkbox size="small" />} label={t('login.form.keepConnected')} componentsProps={{ typography: { fontSize: 12, color: 'text.secondary' } }} />
                <Link component="button" type="button" underline="hover" sx={{ fontSize: 11, fontWeight: 600 }}>{t('login.form.forgotPassword')}</Link>
              </Stack>
              <Button type="submit" fullWidth variant="contained" size="large" disabled={loading} startIcon={loading ? spinner : null} endIcon={!loading && <ArrowForward />} sx={{ mt: 2.5, minHeight: 46 }}>
                {loading ? t('login.form.submitting') : t('login.form.submit')}
              </Button>
              <Typography variant="caption" component="p" sx={{ mt: 2.25, textAlign: 'center' }}>{t(`login.copy.${tab}.footer`)}</Typography>
            </Box>
          )}
        </Paper>

        <Stack direction="row" alignItems="center" spacing={0.9} sx={{ mt: 2.75, color: 'text.disabled' }}>
          <ShieldOutlined sx={{ fontSize: 13 }} />
          <Typography variant="caption">{t('login.secure')}</Typography>
        </Stack>
      </Box>
    </Box>
  );
}
