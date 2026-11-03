import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box, Button, Card, CardContent, CircularProgress, Divider, FormControl,
  Grid, InputLabel, MenuItem, Select, TextField, Typography,
} from '@mui/material';
import { toast } from 'sonner';
import { atribuirAgente, mudarStatus, obterChamado, responderChamado } from '../api/chamados';
import { ApiError } from '../api/errors';
import { STATUS_LABEL, TRANSICOES, type ChamadoDetalheDto, type StatusChamado } from '../types/chamado';
import { StatusChip } from '../components/StatusChip';
import { PriorityChip } from '../components/PriorityChip';
import { SlaBadge } from '../components/SlaBadge';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { TemplatesBar } from '../components/TemplatesBar';
import { EmptyState, ErrorState, PageHeader } from '../components/feedback';
import { useAuth, podeAtribuir } from '../auth/AuthContext';
import { formatarData } from '../utils/sla';
import { mascararEmail } from '../utils/mask';

export function DetalheTicket() {
  const { codigo = '' } = useParams();
  const { sessao } = useAuth();
  const [dados, setDados] = useState<ChamadoDetalheDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState('');
  const [resposta, setResposta] = useState('');
  const [notaInterna, setNotaInterna] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [novoStatus, setNovoStatus] = useState<StatusChamado | ''>('');
  const [confirm, setConfirm] = useState<null | { titulo: string; descricao: string; acao: () => Promise<void> }>(null);
  const [aguarde, setAguarde] = useState(false);

  const carregar = useCallback(async () => {
    setErro('');
    try {
      const d = await obterChamado(codigo);
      setDados(d);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Falha ao carregar ticket.');
    } finally {
      setLoading(false);
    }
  }, [codigo]);

  useEffect(() => {
    setLoading(true);
    carregar();
  }, [carregar]);

  async function enviar(tipo: 'Publica' | 'NotaInterna', texto: string, limpar: () => void) {
    if (!texto.trim()) {
      toast.warning('Escreva a mensagem antes de enviar.');
      return;
    }
    setEnviando(true);
    try {
      await responderChamado(codigo, texto.trim(), tipo);
      toast.success(tipo === 'Publica' ? 'Resposta enviada ao cliente.' : 'Nota interna registrada.');
      limpar();
      await carregar();
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Falha ao enviar.');
    } finally {
      setEnviando(false);
    }
  }

  function pedirConfirmacaoStatus(s: StatusChamado) {
    if (!dados) return;
    const irreversivel = s === 'Fechado';
    setConfirm({
      titulo: `${irreversivel ? 'Encerrar' : 'Mudar status para ' + STATUS_LABEL[s]}?`,
      descricao: `${dados.chamado.codigoPublico}: ${STATUS_LABEL[dados.chamado.status]} → ${STATUS_LABEL[s]}.${irreversivel ? ' Encerrar impede novas respostas até reabrir.' : ''}`,
      acao: async () => {
        setAguarde(true);
        try {
          await mudarStatus(codigo, s);
          toast.success(`Status → ${STATUS_LABEL[s]}.`);
          setNovoStatus('');
          await carregar();
        } catch (e) {
          toast.error(e instanceof ApiError ? e.message : 'Falha ao mudar status.');
        } finally {
          setAguarde(false);
          setConfirm(null);
        }
      },
    });
  }

  async function assumirRapido() {
    setAguarde(true);
    try {
      await atribuirAgente(codigo, undefined, true);
      toast.success('Ticket assumido.');
      await carregar();
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Falha ao assumir.');
    } finally {
      setAguarde(false);
    }
  }

  if (loading) {
    return (
      <Box textAlign="center" py={4} role="status" aria-label="Carregando ticket">
        <CircularProgress />
      </Box>
    );
  }
  if (erro || !dados) return <ErrorState texto={erro || 'Ticket não encontrado.'} onRetry={() => { setLoading(true); carregar(); }} />;

  const { chamado, interacoes } = dados;
  const transicoes = TRANSICOES[chamado.status];

  return (
    <Box>
      <PageHeader titulo={`${chamado.codigoPublico} — ${chamado.titulo}`} subtitulo={`${chamado.produto} · ${chamado.categoria}`} />
      <Grid container spacing={2}>
        <Grid item xs={12} md={8}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Box display="flex" gap={1} flexWrap="wrap" mb={1}>
                <StatusChip status={chamado.status} />
                <PriorityChip prioridade={chamado.prioridade} />
                <SlaBadge slaPrazo={chamado.slaPrazo} status={chamado.status} />
              </Box>
              <Typography variant="body1" sx={{ whiteSpace: 'pre-wrap' }}>{chamado.descricao}</Typography>
              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                Histórico ({interacoes.length})
              </Typography>
              {interacoes.length === 0 ? (
                <EmptyState texto="Sem interações ainda." />
              ) : (
                <div className="timeline">
                  {interacoes.map((i) => (
                    <div key={i.id} className={`timeline-item${i.tipo === 'NotaInterna' ? ' interna' : ''}`}>
                      <Typography variant="caption" color="text.secondary">
                        {formatarData(i.criadoEm)} · {i.tipo === 'NotaInterna' ? 'Nota interna (equipe)' : 'Pública'} · usuário #{i.usuarioId}
                      </Typography>
                      <Typography sx={{ whiteSpace: 'pre-wrap' }}>{i.mensagem}</Typography>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                Responder ao cliente
              </Typography>
              <TemplatesBar onUsar={(t) => setResposta((r) => (r ? `${r}\n\n${t}` : t))} />
              <TextField
                multiline
                rows={4}
                fullWidth
                value={resposta}
                onChange={(e) => setResposta(e.target.value)}
                placeholder="Resposta visível ao cliente…"
                aria-label="Resposta ao cliente"
              />
              <Box mt={1}>
                <Button variant="contained" disabled={enviando} onClick={() => enviar('Publica', resposta, () => setResposta(''))}>
                  {enviando ? 'Enviando…' : 'Enviar resposta'}
                </Button>
              </Box>
              <Divider sx={{ my: 2 }} />
              <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                Nota interna (só equipe)
              </Typography>
              <TextField
                multiline
                rows={3}
                fullWidth
                value={notaInterna}
                onChange={(e) => setNotaInterna(e.target.value)}
                placeholder="Contexto, hipótese, pendência…"
                aria-label="Nota interna"
              />
              <Box mt={1}>
                <Button variant="outlined" color="warning" disabled={enviando} onClick={() => enviar('NotaInterna', notaInterna, () => setNotaInterna(''))}>
                  Salvar nota interna
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} gutterBottom>
                Ações
              </Typography>
              <FormControl fullWidth size="small" sx={{ mb: 1 }}>
                <InputLabel>Mudar status</InputLabel>
                <Select
                  value={novoStatus}
                  label="Mudar status"
                  onChange={(e) => setNovoStatus(e.target.value as StatusChamado)}
                >
                  {transicoes.map((s) => (
                    <MenuItem key={s} value={s}>
                      {STATUS_LABEL[s]}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                fullWidth
                variant="outlined"
                disabled={!novoStatus}
                onClick={() => novoStatus && pedirConfirmacaoStatus(novoStatus)}
                sx={{ mb: 1 }}
              >
                Aplicar status
              </Button>
              {podeAtribuir(sessao?.papel) && (
                <Button fullWidth variant="outlined" disabled={aguarde} onClick={assumirRapido}>
                  Assumir (auto)
                </Button>
              )}
              <Divider sx={{ my: 2 }} />
              <Typography variant="body2" color="text.secondary">
                Cliente: usuário #{chamado.usuarioId} · grupo #{chamado.grupoEmpresaId}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Agente: {chamado.agenteId ? `#${chamado.agenteId}` : 'não atribuído'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Criado em {formatarData(chamado.criadoEm)}
              </Typography>
              {sessao?.email && (
                <Typography variant="body2" color="text.secondary">
                  Logado como {mascararEmail(sessao.email)}
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <ConfirmDialog
        aberto={Boolean(confirm)}
        titulo={confirm?.titulo || ''}
        descricao={confirm?.descricao || ''}
        confirmLabel="Confirmar"
        carregando={aguarde}
        onFechar={() => setConfirm(null)}
        onConfirmar={() => confirm?.acao()}
      />
    </Box>
  );
}
