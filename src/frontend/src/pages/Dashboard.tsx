import { useEffect, useState } from 'react';
import { Box, Card, CardContent, CircularProgress, Grid, Typography } from '@mui/material';
import { listarChamados } from '../api/chamados';
import { ApiError } from '../api/errors';
import type { ChamadoDto } from '../types/chamado';
import { EmptyState, ErrorState, PageHeader } from '../components/feedback';
import { slaEstourado } from '../utils/sla';

export function Dashboard() {
  const [itens, setItens] = useState<ChamadoDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState('');

  async function carregar() {
    setErro('');
    try {
      setItens(await listarChamados());
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Falha ao carregar.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    carregar();
  }, []);

  if (loading) {
    return (
      <Box textAlign="center" py={4} role="status" aria-label="Carregando dashboard">
        <CircularProgress />
      </Box>
    );
  }
  if (erro) return <ErrorState texto={erro} onRetry={() => { setLoading(true); carregar(); }} />;
  if (itens.length === 0) return (<><PageHeader titulo="Dashboard" /><EmptyState texto="Sem tickets." /></>);

  const cont = (s: string) => itens.filter((t) => t.status === s).length;
  const estourados = itens.filter((t) => slaEstourado(t.slaPrazo, t.status)).length;
  const semAgente = itens.filter((t) => t.agenteId === null).length;

  const cards: [string, number][] = [
    ['Abertos', cont('Aberto')],
    ['Em andamento', cont('EmAndamento')],
    ['Aguardando cliente', cont('AguardandoCliente')],
    ['Resolvidos', cont('Resolvido')],
    ['SLA estourado', estourados],
    ['Sem agente', semAgente],
  ];

  return (
    <Box>
      <PageHeader titulo="Dashboard" subtitulo="Visão rápida da operação (dados reais da fila)." />
      <Grid container spacing={2}>
        {cards.map(([label, v]) => (
          <Grid item xs={6} md={4} key={label}>
            <Card>
              <CardContent>
                <Typography variant="h4" fontWeight={700}>
                  {v}
                </Typography>
                <Typography color="text.secondary">{label}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
