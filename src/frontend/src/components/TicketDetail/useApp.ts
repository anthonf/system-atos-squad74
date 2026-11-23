import { yupResolver } from '@hookform/resolvers/yup';
import { useTheme } from '@mui/material/styles';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import * as yup from 'yup';
import {
  atribuirChamado,
  chamadoDtoParaTicket,
  consultarChamado,
  listarAgentes,
  mudarStatusChamado,
  responderChamado,
  STATUS_API,
  type AgenteDto,
} from '../../api';
import { STAGES } from '../../data';
import { notifyApiError } from '../../notify';
import type { Role, Session, StatusKey, Ticket } from '../../types';

const messageSchema = yup.object({ text: yup.string().trim().required('validation.required') });
export type MessageFormValues = yup.InferType<typeof messageSchema>;
export const STATUS_OPTIONS: StatusKey[] = ['aberto', 'em_andamento', 'aguardando_cliente', 'resolvido', 'fechado'];

export interface TicketDetailProps {
  ticket: Ticket;
  role: Role;
  user: Session;
  onBack: () => void;
  onUpdate: (ticket: Ticket) => void;
}

export function useApp({ ticket, role, user, onUpdate }: TicketDetailProps) {
  const { atos } = useTheme();
  const { t } = useTranslation();
  const [tab, setTab] = useState('conversa');
  const [busy, setBusy] = useState(false);
  const [agentes, setAgentes] = useState<AgenteDto[]>([]);
  const isTeam = role !== 'cliente';
  const podeGerenciar = role === 'supervisor' || role === 'admin';
  const isMine = ticket.agenteId === user.usuarioId;
  const fechado = ticket.status === 'fechado';
  const team = agentes.filter((a) => a.id !== ticket.agenteId);
  const idx = STAGES.indexOf(ticket.status === 'aguardando_cliente' ? 'em_andamento' : ticket.status);
  const activeStep = ticket.status === 'fechado' ? STAGES.length : idx;
  const podeAssumir = isTeam && !isMine && (!ticket.agenteId || podeGerenciar) && !fechado;

  useEffect(() => {
    if (!isTeam || !podeGerenciar) return;
    let ativo = true;
    listarAgentes()
      .then((lista) => { if (ativo) setAgentes(lista); })
      .catch((err) => notifyApiError(err, t('detail.agentsLoadError')));
    return () => { ativo = false; };
  }, [isTeam, podeGerenciar, t]);

  const recarregar = useCallback(async () => {
    const detalhe = await consultarChamado(ticket.id);
    onUpdate(chamadoDtoParaTicket(detalhe.chamado, user, detalhe.interacoes));
  }, [ticket.id, user, onUpdate]);

  const executar = async (operacao: () => Promise<unknown>, mensagem: string) => {
    setBusy(true);
    try {
      await operacao();
      toast.success(mensagem);
      try {
        await recarregar();
      } catch (err) {
        notifyApiError(err, t('detail.loadError'));
      }
      return true;
    } catch (err) {
      notifyApiError(err, t('detail.actionError'));
      return false;
    } finally {
      setBusy(false);
    }
  };

  const send = async (texto: string) => {
    if (fechado) return false;
    return executar(() => responderChamado(ticket.id, texto, 'Publica'), t(isTeam ? 'detail.toasts.replyTeam' : 'detail.toasts.replyClient'));
  };

  const addNote = async (texto: string) => {
    if (!isTeam || fechado) return false;
    return executar(() => responderChamado(ticket.id, texto, 'NotaInterna'), t('detail.toasts.noteSaved'));
  };

  const setStatus = async (s: StatusKey) => {
    if (s === ticket.status) return;
    await executar(() => mudarStatusChamado(ticket.id, STATUS_API[s]), t('detail.toasts.statusUpdated', { status: t(`status.${s}`) }));
  };

  const assumir = () => executar(
    () => atribuirChamado(ticket.id, { agenteId: user.usuarioId }),
    t('detail.toasts.taken'),
  );
  const repassar = (agenteId: number) => executar(
    () => atribuirChamado(ticket.id, { agenteId }),
    t('detail.toasts.reassigned', { agent: agentes.find((a) => a.id === agenteId)?.nome ?? '' }),
  );
  const desatribuir = () => executar(
    () => atribuirChamado(ticket.id, { desatribuir: true }),
    t('detail.toasts.unassigned'),
  );
  const atribuirAutomaticamente = () => executar(
    () => atribuirChamado(ticket.id, { automatico: true }),
    t('detail.toasts.autoAssigned'),
  );

  return {
    atos, t, tab, setTab, busy, fechado, isTeam, isMine, podeGerenciar, podeAssumir,
    agentes, team, activeStep, send, addNote, setStatus, assumir, repassar, desatribuir, atribuirAutomaticamente,
  };
}

export function useMessageForm(onSubmit: (text: string) => Promise<boolean>) {
  const { control, handleSubmit, reset, formState: { isSubmitting } } = useForm<MessageFormValues>({
    resolver: yupResolver(messageSchema),
    mode: 'onTouched',
    defaultValues: { text: '' },
  });
  const submit = async ({ text }: MessageFormValues) => {
    if (await onSubmit(text)) reset();
  };

  return { control, handleSubmit, submit, isSubmitting };
}
