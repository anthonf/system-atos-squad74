import { useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Alert, Box, Button, Card, CardContent, TextField } from '@mui/material';
import { toast } from 'sonner';
import { useAuth } from '../auth/AuthContext';
import { PageHeader } from '../components/feedback';

export function Login() {
  const { entrar, aviso } = useAuth();
  const nav = useNavigate();
  const loc = useLocation() as { state?: { from?: string } };
  const [jwt, setJwt] = useState('');
  const [erro, setErro] = useState('');
  const [validando, setValidando] = useState(false);
  const enviando = useRef(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (enviando.current) return;
    enviando.current = true;
    setValidando(true);
    setErro('');
    try {
      await entrar(jwt);
      toast.success('Sessão iniciada.');
      nav(loc.state?.from || '/fila', { replace: true });
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Token inválido.');
    } finally {
      enviando.current = false;
      setValidando(false);
    }
  }

  return (
    <Box maxWidth={560} mx="auto" mt={6}>
      <PageHeader titulo="Entrar — Painel do Atendente" subtitulo="Cole o JWT de desenvolvimento (script mint-token.ps1). Em produção, virá do SSO do portal." />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit}>
            <TextField
              label="JWT (Bearer)"
              multiline
              rows={4}
              fullWidth
              value={jwt}
              onChange={(e) => setJwt(e.target.value)}
              placeholder="eyJhbGciOi..."
              aria-label="Token JWT"
              required
              disabled={validando}
            />
            {aviso && !erro && <Alert severity="warning" sx={{ mt: 2 }}>{aviso}</Alert>}
            {erro && (
              <Alert severity="error" sx={{ mt: 2 }} role="alert">
                {erro}
              </Alert>
            )}
            <Alert severity="info" sx={{ mt: 2 }}>
              Gere com: <code>.\scripts\mint-token.ps1 -Sub portal-agente -Email agente@atos.com -Name &quot;Agente&quot; -GrupoId grupo-1 -GrupoNome &quot;Grupo Norte&quot;</code>
              <br />
              Token fica só em memória (não persiste no disco).
            </Alert>
            <Button type="submit" variant="contained" fullWidth sx={{ mt: 2 }} disabled={validando}>
              {validando ? 'Validando acesso…' : 'Entrar'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
}
