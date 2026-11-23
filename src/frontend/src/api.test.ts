import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  chamadoDtoParaTicket,
  criarChamado,
  listarChamados,
  statusDtoParaKey,
  type ChamadoDto,
  type InteracaoDto,
} from './api';
import { ensureAccessToken, expireSession, getAuthEpoch, getTokenVersion, refreshSession } from './auth';
import { send } from './http';
import type { Session } from './types';

vi.mock('./auth', () => ({
  ensureAccessToken: vi.fn(),
  expireSession: vi.fn(),
  getAuthEpoch: vi.fn(),
  getTokenVersion: vi.fn(),
  refreshSession: vi.fn(),
}));

vi.mock('./http', async (importOriginal) => {
  const original = await importOriginal<typeof import('./http')>();
  return { ...original, send: vi.fn() };
});

const json = (payload: unknown, status = 200) => new Response(JSON.stringify(payload), {
  status,
  headers: { 'Content-Type': 'application/json' },
});

const usuario: Session = { usuarioId: 2, grupoEmpresaId: 1, role: 'agente', nome: 'Marina Alves', empresa: null };

const dto: ChamadoDto = {
  id: 7,
  codigoPublico: 'AC-2026-00007',
  usuarioId: 1,
  grupoEmpresaId: 1,
  agenteId: 2,
  produto: 'SAP Business One',
  categoria: 'Integração ERP',
  titulo: 'Sincronização parada',
  descricao: 'Pedidos não sincronizam.',
  status: 'EmAndamento',
  prioridade: 'Alta',
  slaPrazo: '2026-11-12T10:00:00',
  criadoEm: '2026-11-11T10:00:00',
  usuarioNome: 'Fernanda Duarte',
  grupoEmpresaNome: 'Banco Confiança',
  agenteNome: 'Marina Alves',
};

const interacoes: InteracaoDto[] = [
  { id: 1, usuarioId: 1, tipo: 'Publica', mensagem: 'Pedidos não sincronizam.', criadoEm: '2026-11-11T10:00:00', usuarioNome: 'Fernanda Duarte', papelUsuario: 'Cliente' },
  { id: 2, usuarioId: 2, tipo: 'Publica', mensagem: 'Estamos investigando.', criadoEm: '2026-11-11T11:00:00', usuarioNome: 'Marina Alves', papelUsuario: 'Agente' },
  { id: 3, usuarioId: 2, tipo: 'NotaInterna', mensagem: 'Verificar fila de integração.', criadoEm: '2026-11-11T11:05:00', usuarioNome: 'Marina Alves', papelUsuario: 'Agente' },
];

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(ensureAccessToken).mockResolvedValue('token-de-teste');
  vi.mocked(getAuthEpoch).mockReturnValue(0);
  vi.mocked(getTokenVersion).mockReturnValue(0);
  vi.mocked(send).mockResolvedValue(json({}));
});

describe('cliente da API', () => {
  it('abre chamados pelo endpoint interno com a identidade da equipe', async () => {
    vi.mocked(send).mockResolvedValue(json({ ...dto }, 201));
    await criarChamado({ titulo: '  Sincronização parada  ', produto: 'SAP Business One', categoria: 'Integração ERP', descricao: '  Pedidos não sincronizam.  ' });
    expect(send).toHaveBeenCalledWith('/api/chamados/interno', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ titulo: 'Sincronização parada', produto: 'SAP Business One', categoria: 'Integração ERP', mensagemInicial: 'Pedidos não sincronizam.' }),
    }));
  });

  it('envia filtros da fila como query string', async () => {
    vi.mocked(send).mockResolvedValue(json([]));
    await listarChamados({ status: 'Aberto', agenteId: 2 });
    expect(vi.mocked(send).mock.calls[0][0]).toBe('/api/chamados?status=Aberto&agenteId=2');
  });

  it('renova o token uma vez e repete a requisição após 401', async () => {
    vi.mocked(refreshSession).mockResolvedValue(usuario);
    vi.mocked(send)
      .mockResolvedValueOnce(new Response('', { status: 401 }))
      .mockResolvedValueOnce(json([]));
    await listarChamados();
    expect(refreshSession).toHaveBeenCalledTimes(1);
    expect(send).toHaveBeenCalledTimes(2);
    expect(expireSession).not.toHaveBeenCalled();
  });
});

describe('chamadoDtoParaTicket', () => {
  it('usa nomes reais, calcula SLA em horas e separa mensagens de notas', () => {
    const ticket = chamadoDtoParaTicket(dto, usuario, interacoes);
    expect(ticket).toMatchObject({
      id: 'AC-2026-00007',
      empresa: 'Banco Confiança',
      contato: 'Fernanda Duarte',
      agente: 'Marina Alves',
      agenteId: 2,
      slaTotal: 24,
      status: 'em_andamento',
    });
    expect(ticket.mensagens.map((m) => m.autor)).toEqual(['Fernanda Duarte', 'Marina Alves']);
    expect(ticket.mensagens[1].tipo).toBe('agente');
    expect(ticket.notas).toEqual([expect.objectContaining({ autor: 'Marina Alves', tipoInteracao: 'NotaInterna' })]);
  });

  it('normaliza status acentuado e sem acento', () => {
    expect(statusDtoParaKey('Aguardando Cliente')).toBe('aguardando_cliente');
    expect(statusDtoParaKey('AguardandoCliente')).toBe('aguardando_cliente');
  });
});
