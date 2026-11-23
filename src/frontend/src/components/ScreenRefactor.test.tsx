import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { ThemeProvider } from '@mui/material/styles';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { beforeEach, expect, it, vi } from 'vitest';
import i18n from '../i18n';
import { atribuirChamado, chamadoDtoParaTicket, consultarChamado, listarAgentes, responderChamado } from '../api';
import { ApiError } from '../http';
import { ticketFixture } from '../test/fixtures';
import { buildTheme } from '../theme';
import TicketList from './TicketList';
import TicketDetail from './TicketDetail';
import type { Session, ThemeMode } from '../types';

vi.mock('../api', async (importOriginal) => ({
  ...await importOriginal<typeof import('../api')>(),
  responderChamado: vi.fn(), consultarChamado: vi.fn(), chamadoDtoParaTicket: vi.fn(),
  listarAgentes: vi.fn(), atribuirChamado: vi.fn(),
}));
beforeEach(async () => {
  vi.resetAllMocks();
  await i18n.changeLanguage('pt');
  vi.mocked(listarAgentes).mockResolvedValue([]);
  vi.mocked(consultarChamado).mockResolvedValue({ chamado: {} as never, interacoes: [] });
  vi.mocked(chamadoDtoParaTicket).mockReturnValue(ticketFixture());
});

const CLIENTE: Session = { usuarioId: 1, grupoEmpresaId: 1, nome: 'Fernanda Duarte', empresa: 'Banco Confiança', role: 'cliente' };
const SEM_GRUPO: Session = { usuarioId: 9, grupoEmpresaId: null, nome: 'Marina Alves', empresa: null, role: 'agente' };

it.each<ThemeMode>(['light', 'dark'])('lista mantém busca, escopo e abertura no tema %s', (mode) => {
  const ticket = ticketFixture();
  const outros = [
    ticketFixture({ id: 'AC-2026-00215', grupoEmpresaId: 2, empresa: CLIENTE.empresa!, assunto: 'Chamado de outra empresa' }),
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

it.each<ThemeMode>(['light', 'dark'])('detalhe usa resposta e status confirmados pelo backend no tema %s', async (mode) => {
  const ticket = ticketFixture({ status: 'aberto' });
  const persisted = ticketFixture({ status: 'aberto', mensagens: [...ticket.mensagens, {
    autor: CLIENTE.nome, autorId: 1, tipo: 'cliente', tipoInteracao: 'Publica',
    data: '2026-11-23T12:00:00Z', texto: 'Atualização do cliente',
  }] });
  vi.mocked(chamadoDtoParaTicket).mockReturnValue(persisted);
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
  expect(responderChamado).toHaveBeenCalledWith(ticket.id, 'Atualização do cliente', 'Publica');
  expect(updated.status).toBe('aberto');
  expect(updated.mensagens).toHaveLength(ticket.mensagens.length + 1);
  expect(updated.mensagens.at(-1)).toMatchObject({ autor: CLIENTE.nome, tipo: 'cliente', texto: 'Atualização do cliente' });
  expect(screen.getByRole('textbox', { name: /Mensagem/ })).toHaveValue('');
});

it.each(['cliente', 'agente'] as const)('preserva a resposta de %s após falha e permite reenviar', async (role) => {
  vi.mocked(responderChamado).mockRejectedValueOnce(new ApiError('Falha', 500)).mockResolvedValueOnce({} as never);
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture()} user={{ ...CLIENTE, role }} role={role} onBack={vi.fn()} onUpdate={vi.fn()} />
  </ThemeProvider>);
  const input = screen.getByRole('textbox', { name: /Mensagem/ });
  fireEvent.change(input, { target: { value: 'Não perder' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(input).toBeEnabled());
  expect(input).toHaveValue('Não perder');
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(input).toHaveValue(''));
  expect(responderChamado).toHaveBeenCalledTimes(2);
});

it('mantém o texto e não atualiza localmente enquanto o envio aguarda confirmação', async () => {
  let confirmar = () => {};
  vi.mocked(responderChamado).mockReturnValueOnce(new Promise((resolve) => {
    confirmar = () => resolve({} as never);
  }));
  const onUpdate = vi.fn();
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture()} user={CLIENTE} role="cliente" onBack={vi.fn()} onUpdate={onUpdate} />
  </ThemeProvider>);
  const input = screen.getByRole('textbox', { name: /Mensagem/ });
  fireEvent.change(input, { target: { value: 'Aguardando API' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(responderChamado).toHaveBeenCalledOnce());
  expect(input).toHaveValue('Aguardando API');
  expect(onUpdate).not.toHaveBeenCalled();
  await act(async () => confirmar());
  await waitFor(() => expect(input).toHaveValue(''));
  expect(onUpdate).toHaveBeenCalledOnce();
});

it('limpa resposta confirmada mesmo se a atualização do detalhe falhar', async () => {
  vi.mocked(consultarChamado).mockRejectedValue(new ApiError('Falha', 503));
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture()} user={CLIENTE} role="cliente" onBack={vi.fn()} onUpdate={vi.fn()} />
  </ThemeProvider>);
  const input = screen.getByRole('textbox', { name: /Mensagem/ });
  fireEvent.change(input, { target: { value: 'Persistida' } });
  fireEvent.click(screen.getByRole('button', { name: 'Enviar' }));
  await waitFor(() => expect(input).toHaveValue(''));
  expect(responderChamado).toHaveBeenCalledOnce();
});

it('preserva a nota interna após falha e limpa após confirmação', async () => {
  vi.mocked(responderChamado).mockRejectedValueOnce(new ApiError('Falha', 500)).mockResolvedValueOnce({} as never);
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture()} user={SEM_GRUPO} role="agente" onBack={vi.fn()} onUpdate={vi.fn()} />
  </ThemeProvider>);
  fireEvent.click(screen.getByRole('tab', { name: i18n.t('detail.tabNotes') }));
  const input = screen.getByRole('textbox', { name: new RegExp(i18n.t('detail.note')) });
  fireEvent.change(input, { target: { value: 'Nota preservada' } });
  fireEvent.click(screen.getByRole('button', { name: i18n.t('detail.saveNote') }));
  await waitFor(() => expect(input).toBeEnabled());
  expect(input).toHaveValue('Nota preservada');
  fireEvent.click(screen.getByRole('button', { name: i18n.t('detail.saveNote') }));
  await waitFor(() => expect(input).toHaveValue(''));
  expect(responderChamado).toHaveBeenLastCalledWith(ticketFixture().id, 'Nota preservada', 'NotaInterna');
});

it('permite atribuição automática mesmo com falha na lista de agentes', async () => {
  vi.mocked(listarAgentes).mockRejectedValue(new ApiError('Falha', 503));
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture({ agenteId: null, agente: null })} user={{ ...CLIENTE, role: 'supervisor' }} role="supervisor" onBack={vi.fn()} onUpdate={vi.fn()} />
  </ThemeProvider>);
  const button = screen.getByRole('button', { name: i18n.t('detail.autoAssign') });
  expect(button).toBeEnabled();
  fireEvent.click(button);
  await waitFor(() => expect(atribuirChamado).toHaveBeenCalledWith(ticketFixture().id, { automatico: true }));
});

it('permite desatribuir sem outros agentes disponíveis', async () => {
  render(<ThemeProvider theme={buildTheme('light')}>
    <TicketDetail ticket={ticketFixture()} user={{ ...CLIENTE, role: 'supervisor' }} role="supervisor" onBack={vi.fn()} onUpdate={vi.fn()} />
  </ThemeProvider>);
  const select = screen.getByRole('combobox', { name: i18n.t('detail.reassign') });
  expect(select).toBeEnabled();
  fireEvent.mouseDown(select);
  fireEvent.click(await screen.findByRole('option', { name: i18n.t('detail.leaveUnassigned') }));
  await waitFor(() => expect(atribuirChamado).toHaveBeenCalledWith(ticketFixture().id, { desatribuir: true }));
});
