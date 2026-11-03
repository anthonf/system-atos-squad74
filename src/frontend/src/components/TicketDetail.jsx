// Detalhe do chamado: conversa, notas internas, SLA, ciclo de vida e gestão (assumir / repassar).
import React, { useState } from 'react';
import {
  Box, Button, MenuItem, Paper, Stack, Step, StepLabel, Stepper, Tab, Tabs, TextField, Typography,
} from '@mui/material';
import { useTheme } from '@mui/material/styles';
import { ChevronLeft, LockOutlined, Send } from '@mui/icons-material';
import { STAGES, STATUS } from '../data';
import { Eyebrow, SlaBar, StatusChip } from './ui';
import { MONO } from '../theme';

function Message({ autor, data, texto, tint, note }) {
  const { atos } = useTheme();
  return (
    <Paper variant="outlined" sx={{ p: 2, bgcolor: tint ? atos.surfaceAlt : 'background.paper', ...(note && { borderLeft: 3, borderLeftColor: 'text.disabled' }) }}>
      <Stack direction="row" justifyContent="space-between" mb={0.75}>
        <Typography sx={{ fontWeight: 600, fontSize: 13 }}>{autor}</Typography>
        <Typography sx={{ fontFamily: MONO, fontSize: 11, color: 'text.disabled' }}>{data}</Typography>
      </Stack>
      <Typography sx={{ lineHeight: 1.55 }}>{texto}</Typography>
    </Paper>
  );
}

export default function TicketDetail({ ticket, role, user, onBack, onUpdate, notify }) {
  const { atos } = useTheme();
  const [reply, setReply] = useState('');
  const [note, setNote] = useState('');
  const [tab, setTab] = useState('conversa');
  const [sending, setSending] = useState(false);
  const isAgent = role === 'agente';
  const isMine = ticket.agente === user.nome || (ticket.agenteId && ticket.agenteId === user.usuarioId);
  const idx = STAGES.indexOf(ticket.status === 'aguardando_cliente' ? 'em_andamento' : ticket.status);
  const activeStep = ticket.status === 'fechado' ? STAGES.length : idx;

  const send = async () => {
    if (!reply.trim() || sending) return;
    setSending(true);
    try {
      await onUpdate({ ...ticket, _reply: reply.trim() }, 'responder');
      setReply('');
    } catch (err) {
      // erro já notificado no pai
    } finally {
      setSending(false);
    }
  };

  const addNote = async () => {
    if (!note.trim() || sending) return;
    setSending(true);
    try {
      await onUpdate({ ...ticket, _note: note.trim() }, 'nota');
      setNote('');
    } catch (err) {
    } finally {
      setSending(false);
    }
  };

  const setStatusAction = async (s) => {
    try {
      await onUpdate({ ...ticket, _novoStatus: s }, 'status');
    } catch (err) {
    }
  };

  const assign = async (a) => {
    try {
      if (!a || a === '__none__') {
        await onUpdate({ ...ticket, _targetAgenteId: null }, 'repassar');
        return;
      }
      // Se for o próprio nome/usuário, assume
      if (a === user.nome || a === user.usuarioId) {
        await onUpdate({ ...ticket }, 'assumir');
        return;
      }
      await onUpdate({ ...ticket, _targetAgenteId: typeof a === 'number' ? a : a }, 'repassar');
    } catch (err) {
    }
  };

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Button onClick={onBack} startIcon={<ChevronLeft />} sx={{ mb: 2, color: 'text.secondary', minHeight: 32, px: 0.5 }}>Voltar</Button>

      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" spacing={2} mb={0.5}>
        <Box>
          <Typography sx={{ fontFamily: MONO, fontSize: 12, color: 'text.disabled', mb: 0.5 }}>{ticket.id}</Typography>
          <Typography variant="h1">{ticket.categoria} · {ticket.produto}</Typography>
          {ticket.assunto && <Typography color="text.secondary" mt={0.5}>{ticket.assunto}</Typography>}
        </Box>
        <StatusChip status={ticket.status} />
      </Stack>
      <Typography color="text.secondary" mb={3}>{ticket.empresa} — aberto por {ticket.contato} em {ticket.abertura}</Typography>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '2fr 1fr' }, gap: 4 }}>
        <Box>
          {isAgent && (
            <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}>
              <Tab value="conversa" label="Conversa com o cliente" />
              <Tab value="notas" label="Notas internas" icon={<LockOutlined sx={{ fontSize: 14 }} />} iconPosition="start" />
            </Tabs>
          )}

          {tab === 'conversa' ? (
            <>
              <Stack spacing={2} mb={2}>
                {ticket.mensagens.map((m, i) => <Message key={i} {...m} tint={m.tipo === 'agente'} />)}
              </Stack>
              <TextField value={reply} onChange={(e) => setReply(e.target.value)} multiline minRows={3} fullWidth
                placeholder={isAgent ? 'Responder ao cliente' : 'Adicionar uma atualização ao chamado'} sx={{ mb: 1.25 }} />
              <Button variant="contained" startIcon={<Send fontSize="small" />} onClick={send} disabled={sending || !reply.trim()}>Enviar</Button>
            </>
          ) : (
            <>
              <Stack spacing={1.5} mb={2}>
                {ticket.notas.length === 0 ? (
                  <Paper variant="outlined" sx={{ py: 4, textAlign: 'center' }}>
                    <LockOutlined sx={{ color: 'text.disabled' }} />
                    <Typography variant="body2" color="text.disabled">Nenhuma nota interna registrada.</Typography>
                  </Paper>
                ) : ticket.notas.map((n, i) => <Message key={i} {...n} tint note />)}
              </Stack>
              <TextField value={note} onChange={(e) => setNote(e.target.value)} multiline minRows={3} fullWidth placeholder="Nota visível apenas para a equipe de suporte" sx={{ mb: 1.25 }} />
              <Button variant="contained" color="inherit" onClick={addNote} disabled={sending || !note.trim()} sx={{ bgcolor: 'text.primary', color: 'background.paper', '&:hover': { bgcolor: 'text.secondary' } }}>Salvar nota</Button>
            </>
          )}
        </Box>

        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ p: 2 }}><SlaBar ticket={ticket} /></Paper>

          {ticket.status === 'aguardando_cliente' && (
            <Paper variant="outlined" sx={{ p: 2, bgcolor: atos.purpleBg, borderColor: 'transparent' }}>
              <Typography variant="body2">
                Aguardando retorno desde {ticket.aguardandoDesde}. Sem resposta até {ticket.prazoAutoEncerramento}, o chamado será encerrado automaticamente.
              </Typography>
            </Paper>
          )}

          <Paper variant="outlined" sx={{ p: 2 }}>
            <Eyebrow color="text.secondary">Ciclo de vida</Eyebrow>
            <Stepper activeStep={activeStep} orientation="vertical" sx={{ mt: 1 }}>
              {STAGES.map((s) => (
                <Step key={s}>
                  <StepLabel>{STATUS[s].label}</StepLabel>
                </Step>
              ))}
            </Stepper>
          </Paper>

          {isAgent && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Eyebrow color="text.secondary">Gestão do chamado</Eyebrow>
              <Typography variant="caption" component="p" mt={1}>Responsável atual</Typography>
              <Typography sx={{ fontWeight: 500, mb: 1.5 }}>
                {ticket.agente || 'Sem agente'}
                {isMine && <Box component="span" sx={{ color: 'success.main' }}> (você)</Box>}
              </Typography>

              {!isMine && <Button fullWidth variant="contained" onClick={() => assign(user.usuarioId || user.nome)} sx={{ mb: 2 }}>Assumir chamado</Button>}

              <Stack spacing={2}>
                <TextField select size="small" fullWidth label="Repassar / Atribuir" value="" onChange={(e) => assign(e.target.value)}>
                  {user.usuarioId && <MenuItem value={user.usuarioId}>Atribuir a mim</MenuItem>}
                  {ticket.agenteId && <MenuItem value="__none__">Deixar sem agente</MenuItem>}
                </TextField>
                <TextField select size="small" fullWidth label="Status" value={ticket.status} onChange={(e) => setStatusAction(e.target.value)}>
                  {Object.entries(STATUS).map(([k, v]) => <MenuItem key={k} value={k}>{v.label}</MenuItem>)}
                </TextField>
              </Stack>
            </Paper>
          )}
        </Stack>
      </Box>
    </Box>
  );
}
