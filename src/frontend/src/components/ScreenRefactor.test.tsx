import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { beforeEach, expect, it, vi } from 'vitest';
import i18n from '../i18n';
import { ticketFixture } from '../test/fixtures';
import { buildTheme } from '../theme';
import TicketList from './TicketList';
import TicketDetail from './TicketDetail';
import type { Session, ThemeMode } from '../types';

beforeEach(async () => { await i18n.changeLanguage('pt'); });

const CLIENTE: Session = { usuarioId: 1, grupoEmpresaId: 1, nome: 'Fernanda Duarte', empresa: 'Banco Confiança', role: 'cliente' };
const SEM_GRUPO: Session = { usuarioId: 9, grupoEmpresaId: null, nome: 'Marina Alves', empresa: null, role: 'agente' };

it.each<ThemeMode>(['light', 'dark'])('lista mantém busca, escopo e abertura no tema %s', (mode) => {
  const ticket = ticketFixture();
  const outros = [
    ticketFixture({ id: 'AC-2026-00215', empresa: 'Outra Empresa', assunto: 'Chamado de outra empresa' }),
    ticketFixture({ id: 'AC-2026-00216', assunto: 'Segundo plano do banco' }),
  ];
  const onOpen = vi.fn();
  render(
    <ThemeProvider theme={buildTheme(mode)}>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <TicketList tickets={[ticket, ...outros]} loading={false} error={false} onRefresh={vi.fn()} user={CLIENTE} role="cliente" onOpen={onOpen} onNew={vi.fn()} />
      </LocalizationProvider>
    </ThemeProvider>,
  );
  const search = screen.getByRole('searchbox', { name: 'Buscar chamados' });
  fireEvent.change(search, { target: { value: 'inexistente-xyz' } });
  expect(screen.queryAllByRole('button', { name: /^Abrir chamado AC-/ })).toHaveLength(0);
  fireEvent.change(search, { target: { value: ticket.id } });
  fireEvent.click(screen.getByRole('button', { name: i18n.t('tickets.openTicket', { id: ticket.id }) }));
  expect(onOpen).toHaveBeenCalledWith(ticket.id);
  fireEvent.change(search, { target: { value: outros[0].id } });
  expect(screen.queryByRole('button', { name: i18n.t('tickets.openTicket', { id: outros[0].id }) })).not.toBeInTheDocument();
});

it('esconde o botão de abrir chamado quando a sessão não tem grupo de empresa', () => {
  render(
    <ThemeProvider theme={buildTheme('light')}>
      <LocalizationProvider dateAdapter={AdapterDayjs}>
        <TicketList tickets={[]} loading={false} error={false} onRefresh={vi.fn()} user={SEM_GRUPO} role="agente" onOpen={vi.fn()} onNew={vi.fn()} />
      </LocalizationProvider>
    </ThemeProvider>,
  );
  expect(screen.queryByRole('button', { name: 'Abrir novo chamado' })).not.toBeInTheDocument();
});

it.each<ThemeMode>(['light', 'dark'])('detalhe mantém resposta e transição do cliente no tema %s', async (mode) => {
  const ticket = ticketFixture();
  const onUpdate = vi.fn();
  render(
    <ThemeProvider theme={buildTheme(mode)}>
      <TicketDetail ticket={ticket} user={CLIENTE} role="cliente" onBack={vi.fn()} onUpdate={onUpdate} />
    </ThemeProvider>,
  );
  fireEvent.change(screen.getByRole('textbox', { name: /Mensagem/ }), { target: { value: 'Atualização do cliente' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(onUpdate).toHaveBeenCalledOnce());
  const updated = onUpdate.mock.calls[0][0];
  expect(updated.status).toBe('em_andamento');
  expect(updated.mensagens).toHaveLength(ticket.mensagens.length + 1);
  expect(updated.mensagens.at(-1)).toMatchObject({ autor: CLIENTE.nome, tipo: 'cliente', texto: 'Atualização do cliente' });
  expect(screen.getByRole('textbox', { name: /Mensagem/ })).toHaveValue('');
});
