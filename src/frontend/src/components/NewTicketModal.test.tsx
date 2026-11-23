// Tela de abertura de chamado: Título obrigatório no front e erro da API exibido sob o campo.
import React from 'react';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '@mui/material/styles';
import i18n from '../i18n';
import { buildTheme } from '../theme';
import { ApiError, criarChamado, listarSlaCategorias, type ChamadoDto } from '../api';
import { NewTicketModal } from './TicketList';
import type { Session } from '../types';

// Só a rede é trocada por mocks; ApiError e o mapeamento do chamado continuam os reais.
vi.mock('../api', async (importOriginal) => {
  const original = await importOriginal<typeof import('../api')>();
  return { ...original, listarSlaCategorias: vi.fn(), criarChamado: vi.fn() };
});

const user: Session = { usuarioId: 9, grupoEmpresaId: 1, role: 'admin', nome: 'Admin Teste', empresa: null, email: 'admin@atoscapital.com' };
const OPCOES = [{ produto: 'Portal', categoria: 'Login', prioridade: 'Alta' }];
const DTO: ChamadoDto = {
  id: 1, codigoPublico: 'CH-0001', usuarioId: 9, grupoEmpresaId: 1, agenteId: null,
  produto: 'Portal', categoria: 'Login', titulo: 'Problema no login',
  descricao: 'Não consigo acessar o sistema.', status: 'Aberto', prioridade: 'Alta',
  slaPrazo: '2026-11-12T10:00:00', criadoEm: '2026-11-11T10:00:00',
  usuarioNome: 'Admin Teste', grupoEmpresaNome: 'Cliente Teste',
};

// O teste valida os valores e a submissao, nao digitacao por tecla.
function preencherTexto(_u: ReturnType<typeof userEvent.setup>, campo: HTMLElement, valor: string) {
  fireEvent.change(campo, { target: { value: valor } });
}

function renderModal() {
  const onCreate = vi.fn();
  render(
    <ThemeProvider theme={buildTheme('light')}>
      <NewTicketModal user={user} onClose={vi.fn()} onCreate={onCreate} />
    </ThemeProvider>,
  );
  return onCreate;
}

/** Preenche categoria e descrição (o produto já vem escolhido); o título cada teste decide. */
async function preencherRestante(u: ReturnType<typeof userEvent.setup>) {
  await u.click(await screen.findByLabelText(/categoria/i));
  await u.click(await screen.findByRole('option', { name: 'Login' }));
  await preencherTexto(u, screen.getByLabelText(/descrição/i), 'Não consigo acessar o sistema.');
}

describe('NewTicketModal — Título obrigatório', () => {
  beforeAll(async () => { await i18n.changeLanguage('pt'); });
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(listarSlaCategorias).mockResolvedValue(OPCOES);
  });

  it('não envia sem título: mostra o erro sob o campo e não chama a API', async () => {
    const u = userEvent.setup();
    renderModal();
    await preencherRestante(u);

    await u.click(screen.getByText('Enviar chamado', { selector: 'button' }));

    const titulo = await screen.findByLabelText(/título/i);
    await waitFor(() => expect(titulo).toHaveAccessibleDescription('Campo obrigatório.'));
    expect(titulo).toBeInvalid();
    expect(criarChamado).not.toHaveBeenCalled();
  });

  it('título só com espaços também é recusado', async () => {
    const u = userEvent.setup();
    renderModal();
    await preencherTexto(u, await screen.findByLabelText(/título/i), '   ');
    await preencherRestante(u);

    await u.click(screen.getByText('Enviar chamado', { selector: 'button' }));

    await waitFor(() => expect(screen.getByLabelText(/título/i)).toHaveAccessibleDescription('Campo obrigatório.'));
    expect(criarChamado).not.toHaveBeenCalled();
  });

  it('mostra, sob o campo Título, o erro que a API devolveu', async () => {
    vi.mocked(criarChamado).mockRejectedValue(new ApiError('O título é obrigatório.', 400, ['O título é obrigatório.'], 'titulo'));
    const u = userEvent.setup();
    const onCreate = renderModal();
    await preencherTexto(u, await screen.findByLabelText(/título/i), 'Problema no login');
    await preencherRestante(u);

    await u.click(screen.getByText('Enviar chamado', { selector: 'button' }));

    await waitFor(() => expect(screen.getByLabelText(/título/i)).toHaveAccessibleDescription('O título é obrigatório.'));
    expect(onCreate).not.toHaveBeenCalled();
  });

  it('com título válido envia (sem espaços nas pontas) e cria o chamado', async () => {
    vi.mocked(criarChamado).mockResolvedValue(DTO);
    const u = userEvent.setup();
    const onCreate = renderModal();
    await preencherTexto(u, await screen.findByLabelText(/título/i), '  Problema no login  ');
    await preencherRestante(u);

    await u.click(screen.getByText('Enviar chamado', { selector: 'button' }));

    await waitFor(() => expect(onCreate).toHaveBeenCalledTimes(1));
    expect(criarChamado).toHaveBeenCalledWith({
      titulo: 'Problema no login', produto: 'Portal', categoria: 'Login', descricao: 'Não consigo acessar o sistema.',
    });
    expect(onCreate.mock.calls[0][0]).toMatchObject({ id: 'CH-0001', assunto: 'Problema no login' });
  });
});
