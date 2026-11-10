// Tela de login: vitrine do produto à esquerda, cartão de acesso à direita.
import React, { useState } from 'react';
import {
  Box, Button, Checkbox, CircularProgress, FormControlLabel, IconButton, InputAdornment, Link, Paper, Stack, Tab, Tabs,
  TextField, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import {
  ArrowForward, DarkModeOutlined, LightModeOutlined, LockOutlined, MailOutline, ShieldOutlined, Visibility, VisibilityOff,
} from '@mui/icons-material';
import { BrandMark, Eyebrow } from './ui';
import { STATUS } from '../data';
import { MONO } from '../theme';

const NOISE = "url(\"data:image/svg+xml,%3Csvg viewBox='0 0 180 180' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='.95' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)' opacity='.18'/%3E%3C/svg%3E\")";
const PINK = '#f4a3b1';

function ProductPreview() {
  const rows = [
    { id: '#00214', status: 'em_andamento', c: ['rgba(255,152,0,0.2)', '#ffa726'] },
    { id: '#00212', status: 'aguardando_cliente', c: ['rgba(186,104,200,0.2)', '#ba68c8'] },
    { id: '#00211', status: 'resolvido', c: ['rgba(102,187,106,0.2)', '#66bb6a'] },
  ];
  const stats = [['Em aberto', '6', '#f2eef0'], ['Próximos do SLA', '3', '#ef5350'], ['Resolvidos', '12', '#66bb6a']];
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
          <Typography sx={{ fontSize: 6, color: '#7d7880', textTransform: 'uppercase', letterSpacing: 0.7 }}>Visão geral</Typography>
          <Typography sx={{ fontSize: 11, fontWeight: 700 }}>Fila de atendimento</Typography>
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
              <Box sx={{ fontSize: 6, fontWeight: 600, px: 0.75, py: 0.25, borderRadius: 9, bgcolor: r.c[0], color: r.c[1] }}>{STATUS[r.status].label}</Box>
            </Stack>
          ))}
        </Box>
      </Box>
    </Box>
  );
}

function Showcase() {
  const { atos } = useTheme();
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
          <ShieldOutlined sx={{ fontSize: 14 }} /><Typography variant="caption" sx={{ color: 'inherit' }}>Ambiente seguro</Typography>
        </Stack>
      </Stack>

      <Box sx={{ width: 'min(100%, 620px)', my: 'auto', py: { xs: 4, lg: 6 } }}>
        <Eyebrow color={PINK} sx={{ letterSpacing: '0.16em' }}>Central de atendimento</Eyebrow>
        <Typography component="h1" sx={{ fontSize: 'clamp(34px, 3.2vw, 52px)', fontWeight: 700, lineHeight: 1.1, letterSpacing: '-1.8px', maxWidth: 590 }}>
          Chamados resolvidos<br />com prazo e{' '}
          <Box component="em" sx={{ fontStyle: 'normal', color: PINK, position: 'relative', '&::after': { content: '""', position: 'absolute', left: 1, right: 0, bottom: -6, height: 5, borderTop: `2px solid ${PINK}`, borderRadius: '50%', transform: 'rotate(-1deg)' } }}>
            clareza.
          </Box>
        </Typography>
        <Typography sx={{ maxWidth: 525, mt: 2.5, mb: 3.5, color: 'rgba(255,255,255,0.7)', lineHeight: 1.65 }}>
          Acompanhe cada chamado, o SLA e as notificações em um único ambiente, integrado aos sistemas que a sua operação já usa.
        </Typography>
        <ProductPreview />
        <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: 3 }}>
          <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.55)', mr: 0.5 }}>Integrado a</Typography>
          {['Joe SFA B1', 'SAP Business One', 'SAP S/4HANA'].map((s) => (
            <Box key={s} sx={{ fontSize: 10, px: 1.25, py: 0.6, borderRadius: 9, border: '1px solid rgba(255,255,255,0.16)', bgcolor: 'rgba(255,255,255,0.06)', color: 'rgba(255,255,255,0.82)' }}>{s}</Box>
          ))}
        </Stack>
      </Box>

      <Stack direction="row" spacing={2.75} sx={{ color: 'rgba(255,255,255,0.45)', fontSize: 10 }}>
        <span>© 2026 Atos Capital</span><span>Privacidade</span><span>Segurança</span>
      </Stack>
    </Box>
  );
}

export default function LoginScreen({ onLogin, themeMode, setThemeMode }) {
  const [tab, setTab] = useState('cliente');
  const [loading, setLoading] = useState(false);
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const [showSenha, setShowSenha] = useState(false);
  const [error, setError] = useState('');
  const isClient = tab === 'cliente';

  const handleSso = () => {
    setLoading(true);
    setTimeout(() => onLogin({ role: 'cliente', nome: 'Fernanda Duarte', empresa: 'Banco Confiança' }), 900);
  };

  const handleAgentSubmit = (e) => {
    e.preventDefault();
    if (!email.includes('@') || senha.length < 4) {
      setError('Informe um e-mail corporativo válido e uma senha com pelo menos 4 caracteres.');
      return;
    }
    setError('');
    setLoading(true);
    const nome = email.split('@')[0].split('.').map((p) => p.charAt(0).toUpperCase() + p.slice(1)).join(' ');
    setTimeout(() => onLogin({ role: 'agente', nome, empresa: null, email }), 700);
  };

  const spinner = <CircularProgress size={16} color="inherit" />;

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'minmax(440px, 1fr) minmax(480px, 1fr)' }, bgcolor: 'background.default' }}>
      <Showcase />

      <Box component="main" sx={{ position: 'relative', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', px: { xs: 2, sm: 5 }, pt: 10, pb: 4.5, minHeight: '100vh' }}>
        <Box sx={{ display: { md: 'none' }, position: 'absolute', top: 24, left: 24 }}><BrandMark onDark={themeMode === 'dark'} width={120} /></Box>
        <Button size="small" variant="outlined" onClick={() => setThemeMode(themeMode === 'light' ? 'dark' : 'light')}
          startIcon={themeMode === 'light' ? <DarkModeOutlined /> : <LightModeOutlined />}
          aria-label="Alternar tema claro e escuro"
          sx={{ position: 'absolute', top: 24, right: 24, borderRadius: 9, minHeight: 32, fontSize: 12, bgcolor: 'background.paper' }}>
          {themeMode === 'light' ? 'Escuro' : 'Claro'}
        </Button>

        <Paper variant="outlined" sx={{ width: 'min(100%, 440px)', p: { xs: 3, sm: 4 }, borderRadius: 4, boxShadow: '0 30px 70px rgba(0,0,0,0.10)' }}>
          <Tabs value={tab} onChange={(_, v) => { setTab(v); setError(''); }} variant="fullWidth" sx={{ mb: 3, borderBottom: 1, borderColor: 'divider' }}>
            <Tab value="cliente" label="Portal do cliente" />
            <Tab value="agente" label="Equipe Atos Capital" />
          </Tabs>

          <Stack direction="row" spacing={2} alignItems="flex-start" mb={3}>
            <Box sx={{ flex: '0 0 auto', width: 42, height: 42, display: 'grid', placeItems: 'center', borderRadius: 3, bgcolor: 'primary.main', color: 'primary.contrastText' }}>
              <ShieldOutlined />
            </Box>
            <Box>
              <Eyebrow>{isClient ? 'Portal do cliente' : 'Acesso da equipe'}</Eyebrow>
              <Typography sx={{ fontSize: 26, fontWeight: 700, lineHeight: 1.2, letterSpacing: '-0.6px' }}>Bem-vindo de volta</Typography>
              <Typography variant="body2" color="text.secondary" mt={0.5}>
                {isClient ? 'Acesse com a sua conta Atos Capital.' : 'Entre para atender os chamados.'}
              </Typography>
            </Box>
          </Stack>

          {isClient ? (
            <Box>
              <Button fullWidth variant="contained" size="large" onClick={handleSso} disabled={loading}
                startIcon={loading ? spinner : <ShieldOutlined />} endIcon={!loading && <ArrowForward />} sx={{ minHeight: 46 }}>
                {loading ? 'Redirecionando para Atos Capital ID' : 'Entrar com Atos Capital ID'}
              </Button>
              <Typography variant="caption" component="p" sx={{ mt: 2.25, textAlign: 'center', lineHeight: 1.5 }}>
                Sua conta é sincronizada automaticamente a partir do login na Atos Capital. Não é necessário criar uma senha aqui.
              </Typography>
              <Typography variant="caption" component="p" sx={{ mt: 1.25, textAlign: 'center' }}>
                Ainda não tem acesso? <Link component="button" type="button" underline="hover" sx={{ fontSize: 11, fontWeight: 600, verticalAlign: 'baseline' }}>Fale com o administrador do seu grupo empresa</Link>
              </Typography>
            </Box>
          ) : (
            <Box component="form" onSubmit={handleAgentSubmit} noValidate>
              <Stack spacing={2}>
                <TextField label="E-mail corporativo" type="email" value={email} onChange={(e) => setEmail(e.target.value)} fullWidth autoComplete="username"
                  placeholder="nome.sobrenome@atoscapital.com"
                  InputProps={{ startAdornment: <InputAdornment position="start"><MailOutline fontSize="small" /></InputAdornment> }} />
                <TextField label="Senha" type={showSenha ? 'text' : 'password'} value={senha} onChange={(e) => setSenha(e.target.value)} fullWidth autoComplete="current-password"
                  InputProps={{
                    startAdornment: <InputAdornment position="start"><LockOutlined fontSize="small" /></InputAdornment>,
                    endAdornment: (
                      <InputAdornment position="end">
                        <IconButton size="small" aria-label={showSenha ? 'Ocultar senha' : 'Mostrar senha'} onClick={() => setShowSenha((s) => !s)}>
                          {showSenha ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                        </IconButton>
                      </InputAdornment>
                    ),
                  }} />
              </Stack>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mt={1}>
                <FormControlLabel control={<Checkbox size="small" />} label="Manter conectado neste dispositivo" componentsProps={{ typography: { fontSize: 12, color: 'text.secondary' } }} />
                <Link component="button" type="button" underline="hover" sx={{ fontSize: 11, fontWeight: 600 }}>Esqueci minha senha</Link>
              </Stack>
              {error && <Typography variant="caption" component="p" sx={{ color: 'error.main', mt: 1 }}>{error}</Typography>}
              <Button type="submit" fullWidth variant="contained" size="large" disabled={loading} startIcon={loading ? spinner : null} endIcon={!loading && <ArrowForward />} sx={{ mt: 2.5, minHeight: 46 }}>
                {loading ? 'Entrando' : 'Entrar no portal'}
              </Button>
              <Typography variant="caption" component="p" sx={{ mt: 2.25, textAlign: 'center' }}>Acesso restrito à equipe de suporte da Atos Capital.</Typography>
            </Box>
          )}
        </Paper>

        <Stack direction="row" alignItems="center" spacing={0.9} sx={{ mt: 2.75, color: 'text.disabled' }}>
          <ShieldOutlined sx={{ fontSize: 13 }} />
          <Typography variant="caption">Ambiente seguro. Acesso restrito a usuários autorizados.</Typography>
        </Stack>
      </Box>
    </Box>
  );
}
