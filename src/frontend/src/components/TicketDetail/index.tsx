import { STATUS_OPTIONS, useApp, useMessageForm, type MessageFormValues, type TicketDetailProps } from './useApp';
// Detalhe do chamado: conversa, notas internas, SLA, ciclo de vida e gestão (assumir / repassar).
import { AutoAwesome, ChevronLeft, LockOutlined, Send } from '@mui/icons-material';
import { Alert, Box, Button, MenuItem, Paper, Stack, Step, StepLabel, Stepper, Tab, Tabs, TextField, Typography } from '@mui/material';
import type { SxProps, Theme } from '@mui/material/styles';
import { useTheme } from '@mui/material/styles';
import React from 'react';
import { STAGES } from '../../data';
import { formatDateTime } from '../../dates';
import { MONO } from '../../tokens';
import type { StatusKey } from '../../types';
import FormTextField from '../form/FormTextField';
import { Eyebrow, SlaBar, StatusChip } from '../ui';

interface MessageProps { autor: string; data: string; texto: string; tint?: boolean; note?: boolean }

function Message({ autor, data, texto, tint, note }: MessageProps) {
  const { atos } = useTheme();
  return (
    <Paper variant="outlined" sx={{ p: 2, bgcolor: tint ? atos.surfaceAlt : 'background.paper', ...(note && { borderLeft: 3, borderLeftColor: 'text.disabled' }) }}>
      <Stack direction="row" justifyContent="space-between" mb={0.75}>
        <Typography sx={{ fontWeight: 600, fontSize: 13 }}>{autor}</Typography>
        <Typography sx={{ fontFamily: MONO, fontSize: 11, color: 'text.disabled' }}>{formatDateTime(data)}</Typography>
      </Stack>
      <Typography sx={{ lineHeight: 1.55 }}>{texto}</Typography>
    </Paper>
  );
}

/* Formulário de texto (resposta / nota): react-hook-form + Yup, campo obrigatório com asterisco e erro sob o campo. */
interface MessageFormProps {
  label: string;
  placeholder: string;
  submitLabel: string;
  disabled?: boolean;
  onSubmit: (text: string) => Promise<boolean>;
  startIcon?: React.ReactNode;
  buttonColor?: 'primary' | 'inherit';
  buttonSx?: SxProps<Theme>;
}

function MessageForm({ label, placeholder, submitLabel, disabled, onSubmit, startIcon, buttonColor = 'primary', buttonSx }: MessageFormProps) {
  const { control, handleSubmit, submit, isSubmitting } = useMessageForm(onSubmit);
  const bloqueado = Boolean(disabled || isSubmitting);

  return (
    <Stack component="form" spacing={1.25} onSubmit={handleSubmit(submit)} noValidate>
      <FormTextField<MessageFormValues> name="text" control={control} required multiline minRows={3} fullWidth label={label} placeholder={placeholder} disabled={bloqueado} />
      <Button type="submit" variant="contained" color={buttonColor} disabled={bloqueado} startIcon={startIcon} sx={{ alignSelf: { xs: 'stretch', sm: 'flex-start' }, ...buttonSx }}>{submitLabel}</Button>
    </Stack>
  );
}

export default function TicketDetail(props: TicketDetailProps) {
  const { ticket, user, onBack } = props;
  const {
    atos, t, tab, setTab, busy, fechado, isTeam, isMine, podeGerenciar, podeAssumir, team,
    activeStep, send, addNote, setStatus, assumir, repassar, desatribuir, atribuirAutomaticamente,
  } = useApp(props);

  return (
    <Box sx={{ maxWidth: 1152 }}>
      <Button onClick={onBack} startIcon={<ChevronLeft />} sx={{ mb: 2, color: 'text.secondary', minHeight: 32, px: 0.5 }}>{t('common.back')}</Button>

      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems="flex-start" spacing={2} mb={0.5}>
        <Box>
          <Typography sx={{ fontFamily: MONO, fontSize: 12, color: 'text.disabled', mb: 0.5 }}>{ticket.id}</Typography>
          <Typography variant="h1">{ticket.categoria} · {ticket.produto}</Typography>
          {ticket.assunto && <Typography color="text.secondary" mt={0.5}>{ticket.assunto}</Typography>}
        </Box>
        <StatusChip status={ticket.status} />
      </Stack>
      <Typography color="text.secondary" mb={3}>
        {t('detail.openedBy', { company: ticket.empresa, contact: ticket.contato, date: formatDateTime(ticket.abertura) })}
      </Typography>

      {fechado && <Alert severity="info" sx={{ mb: 2 }}>{t('detail.closedHint')}</Alert>}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '2fr 1fr' }, gap: 4 }}>
        <Box>
          {isTeam && (
            <Tabs value={tab} onChange={(_, v: string) => setTab(v)} variant="scrollable" allowScrollButtonsMobile sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}>
              <Tab value="conversa" id="tab-conversa" aria-controls="panel-conversa" label={t('detail.tabConversation')} />
              <Tab value="notas" id="tab-notas" aria-controls="panel-notas" label={t('detail.tabNotes')} icon={<LockOutlined sx={{ fontSize: 14 }} />} iconPosition="start" />
            </Tabs>
          )}

          {tab === 'conversa' ? (
            <Box role={isTeam ? 'tabpanel' : undefined} id="panel-conversa" aria-labelledby={isTeam ? 'tab-conversa' : undefined}>
              <Stack spacing={2} mb={2}>
                {ticket.mensagens.map((m, i) => <Message key={i} {...m} tint={m.tipo === 'agente'} />)}
              </Stack>
              <MessageForm label={t('detail.message')} placeholder={isTeam ? t('detail.replyTeamPlaceholder') : t('detail.replyClientPlaceholder')}
                submitLabel={t('detail.send')} disabled={fechado || busy} startIcon={<Send fontSize="small" />} onSubmit={send} />
            </Box>
          ) : (
            <Box role="tabpanel" id="panel-notas" aria-labelledby="tab-notas">
              <Stack spacing={1.5} mb={2}>
                {ticket.notas.length === 0 ? (
                  <Paper variant="outlined" sx={{ py: 4, textAlign: 'center' }}>
                    <LockOutlined sx={{ color: 'text.disabled' }} />
                    <Typography variant="body2" color="text.disabled">{t('detail.noNotes')}</Typography>
                  </Paper>
                ) : ticket.notas.map((n, i) => <Message key={i} {...n} tint note />)}
              </Stack>
              <MessageForm label={t('detail.note')} placeholder={t('detail.notePlaceholder')} submitLabel={t('detail.saveNote')}
                disabled={fechado || busy} onSubmit={addNote} buttonColor="inherit"
                buttonSx={{ bgcolor: 'text.primary', color: 'background.paper', '&:hover': { bgcolor: 'text.secondary' } }} />
            </Box>
          )}
        </Box>

        <Stack spacing={2}>
          <Paper variant="outlined" sx={{ p: 2 }}><SlaBar ticket={ticket} /></Paper>

          {ticket.status === 'aguardando_cliente' && (
            <Paper variant="outlined" sx={{ p: 2, bgcolor: atos.purpleBg, borderColor: 'transparent' }}>
              <Typography variant="body2">
                {t('detail.waiting', {
                  since: ticket.aguardandoDesde ? formatDateTime(ticket.aguardandoDesde) : '—',
                  until: ticket.prazoAutoEncerramento ? formatDateTime(ticket.prazoAutoEncerramento) : '—',
                })}
              </Typography>
            </Paper>
          )}

          <Paper variant="outlined" sx={{ p: 2 }}>
            <Eyebrow color="text.secondary">{t('detail.lifecycle')}</Eyebrow>
            <Stepper activeStep={activeStep} orientation="vertical" sx={{ mt: 1 }}>
              {STAGES.map((s) => (
                <Step key={s}>
                  <StepLabel>{t(`status.${s}`)}</StepLabel>
                </Step>
              ))}
            </Stepper>
          </Paper>

          {isTeam && (
            <Paper variant="outlined" sx={{ p: 2 }}>
              <Eyebrow color="text.secondary">{t('detail.management')}</Eyebrow>
              <Typography variant="caption" component="p" mt={1}>{t('detail.currentOwner')}</Typography>
              <Typography sx={{ fontWeight: 500, mb: 1.5 }}>
                {ticket.agente || t('detail.noAgent')}
                {isMine && <Box component="span" sx={{ color: 'success.main' }}> {t('detail.you')}</Box>}
              </Typography>

              {podeAssumir && <Button fullWidth variant="contained" onClick={() => void assumir()} disabled={busy} sx={{ mb: 1 }}>{t('detail.take')}</Button>}
              {podeGerenciar && !ticket.agenteId && !fechado && (
                <Button fullWidth variant="outlined" startIcon={<AutoAwesome />} onClick={() => void atribuirAutomaticamente()} disabled={busy} sx={{ mb: 2 }}>
                  {t('detail.autoAssign')}
                </Button>
              )}

              <Stack spacing={2}>
                {podeGerenciar && !fechado && (
                  <TextField select size="small" fullWidth label={t('detail.reassign')} value="" onChange={(e) => {
                    if (e.target.value === '__none__') void desatribuir();
                    else void repassar(Number(e.target.value));
                  }} disabled={busy || (team.length === 0 && !ticket.agenteId)}>
                    {team.map((a) => <MenuItem key={a.id} value={a.id}>{a.nome}</MenuItem>)}
                    {ticket.agenteId && <MenuItem value="__none__">{t('detail.leaveUnassigned')}</MenuItem>}
                  </TextField>
                )}
                <TextField select size="small" fullWidth label={t('detail.status')} value={ticket.status} onChange={(e) => void setStatus(e.target.value as StatusKey)} disabled={!isTeam || fechado || busy}>
                  {STATUS_OPTIONS.map((k) => <MenuItem key={k} value={k}>{t(`status.${k}`)}</MenuItem>)}
                </TextField>
              </Stack>
            </Paper>
          )}
        </Stack>
      </Box>
    </Box>
  );
}
