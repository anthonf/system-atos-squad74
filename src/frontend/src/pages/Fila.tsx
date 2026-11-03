import { useCallback, useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Box, Button, Card, FormControl, InputLabel, MenuItem, Select,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, CircularProgress, Paper, Checkbox, FormControlLabel,
} from '@mui/material';
import { toast } from 'sonner';
import { atribuirAgente, listarChamados } from '../api/chamados';
import { ApiError } from '../api/errors';
import { STATUS_LABEL, type ChamadoDto, type StatusChamado } from '../types/chamado';
import { StatusChip } from '../components/StatusChip';
import { PriorityChip } from '../components/PriorityChip';
import { SlaBadge } from '../components/SlaBadge';
import { EmptyState, ErrorState, PageHeader } from '../components/feedback';
import { usePolling } from '../hooks/usePolling';
import { useAtalhosFila } from '../utils/atalhos';
import { formatarData } from '../utils/sla';

const STATUS_OPS: (StatusChamado | '')[] = ['', 'Aberto', 'EmAndamento', 'AguardandoCliente', 'Resolvido', 'Fechado'];

export function Fila() {
  const [itens, setItens] = useState<ChamadoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState('');
  const [busca, setBusca] = useState('');
  const [status, setStatus] = useState<StatusChamado | ''>('');
  const [semAgente, setSemAgente] = useState(false);
  const [assumindo, setAssumindo] = useState<string | null>(null);
  const buscaRef = useRef<HTMLInputElement>(null);

  const carregar = useCallback(async () => {
    setErro('');
    try {
      const data = await listarChamados({
        status: status || undefined,
        semAgente: semAgente || undefined,
      });
      setItens(data);
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Falha ao carregar fila.');
    } finally {
      setLoading(false);
    }
  }, [status, semAgente]);

  useEffect(() => {
    setLoading(true);
    carregar();
  }, [carregar]);

  usePolling(() => carregar(), 30000, !loading);
  useAtalhosFila({
    onBuscar: () => buscaRef.current?.focus(),
    onAtualizar: () => {
      setLoading(true);
      carregar();
    },
  });

  const q = busca.trim().toLowerCase();
  const filtrados = q
    ? itens.filter((t) =>
        `${t.codigoPublico} ${t.titulo} ${t.produto} ${t.categoria}`.toLowerCase().includes(q),
      )
    : itens;

  async function assumir(codigo: string) {
    setAssumindo(codigo);
    try {
      await atribuirAgente(codigo, undefined, true);
      toast.success(`${codigo} assumido (atribuição automática).`);
      await carregar();
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Falha ao assumir.');
    } finally {
      setAssumindo(null);
    }
  }

  return (
    <Box>
      <PageHeader titulo="Fila de tickets" subtitulo="Atendimento em volume: filtre, assuma e responda em poucos cliques. Atalhos: / busca · R atualiza." />
      <Card sx={{ mb: 2 }}>
        <Box display="flex" gap={2} flexWrap="wrap" p={2} alignItems="center">
          <TextField
            label="Buscar (código, título, produto)"
            size="small"
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            inputRef={buscaRef}
            sx={{ minWidth: 260 }}
          />
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Status</InputLabel>
            <Select value={status} label="Status" onChange={(e) => setStatus(e.target.value as StatusChamado | '')}>
              {STATUS_OPS.map((s) => (
                <MenuItem key={s || 'todos'} value={s}>
                  {s ? STATUS_LABEL[s] : 'Todos'}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControlLabel
            control={<Checkbox checked={semAgente} onChange={(e) => setSemAgente(e.target.checked)} />}
            label="Sem agente"
          />
          <Button variant="outlined" onClick={() => { setLoading(true); carregar(); }}>
            Atualizar
          </Button>
        </Box>
      </Card>

      {loading ? (
        <Box textAlign="center" py={4} role="status" aria-label="Carregando fila">
          <CircularProgress />
        </Box>
      ) : erro ? (
        <ErrorState texto={erro} onRetry={() => { setLoading(true); carregar(); }} />
      ) : filtrados.length === 0 ? (
        <EmptyState texto="Nenhum ticket na fila com estes filtros." />
      ) : (
        <TableContainer component={Paper}>
          <Table size="small" aria-label="Fila de tickets">
            <TableHead>
              <TableRow>
                <TableCell>Código</TableCell>
                <TableCell>Título</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Prioridade</TableCell>
                <TableCell>SLA</TableCell>
                <TableCell>Atualizado</TableCell>
                <TableCell>Ação</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filtrados.map((t) => (
                <TableRow key={t.codigoPublico} hover>
                  <TableCell>
                    <Link to={`/chamados/${t.codigoPublico}`}>{t.codigoPublico}</Link>
                  </TableCell>
                  <TableCell>{t.titulo}</TableCell>
                  <TableCell><StatusChip status={t.status} /></TableCell>
                  <TableCell><PriorityChip prioridade={t.prioridade} /></TableCell>
                  <TableCell><SlaBadge slaPrazo={t.slaPrazo} status={t.status} /></TableCell>
                  <TableCell>{t.atualizadoEm ? formatarData(t.atualizadoEm) : formatarData(t.criadoEm)}</TableCell>
                  <TableCell>
                    <Button size="small" variant="outlined" disabled={assumindo === t.codigoPublico} onClick={() => assumir(t.codigoPublico)}>
                      {assumindo === t.codigoPublico ? 'Assumindo…' : 'Assumir'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
}
