import { afterEach, describe, expect, it, vi } from 'vitest';
import { getToken, onSessaoInvalida, request, setToken } from '../api/client';
import { agendarExpiracao, tempoRestanteSessao, validarSessao } from '../auth/session';
import type { SessaoUsuario } from '../types/usuario';

const identidade: SessaoUsuario = {
  usuarioId: 12, nome: 'Ana', email: 'ana@atos.com', papel: 'Agente',
  grupoEmpresaId: 3, grupoEmpresaNome: 'Grupo Norte',
  servidorEm: '2026-10-27T14:00:00Z', expiraEm: '2026-10-27T15:00:00Z',
};

afterEach(() => {
  setToken(null);
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('validação do login pela API', () => {
  it('aguarda a API e usa a identidade provisionada sem ativar o token candidato', async () => {
    let responder!: (res: Response) => void;
    const fetchMock = vi.fn(() => new Promise<Response>((resolve) => { responder = resolve; }));
    vi.stubGlobal('fetch', fetchMock);
    const promessa = validarSessao('candidato');
    expect(getToken()).toBeNull();
    expect(fetchMock).toHaveBeenCalledWith('/api/usuarios/me', expect.objectContaining({
      cache: 'no-store', headers: expect.objectContaining({ Authorization: 'Bearer candidato' }),
    }));
    responder(Response.json(identidade));
    await expect(promessa).resolves.toEqual(identidade);
    expect(getToken()).toBeNull();
  });

  it('rejeita o token quando a API retorna 401', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    await expect(validarSessao('invalido')).rejects.toMatchObject({ status: 401 });
    expect(getToken()).toBeNull();
  });

  it('identidade Cliente válida não abre o painel interno', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ ...identidade, papel: 'Cliente' })));
    await expect(validarSessao('cliente')).rejects.toThrow(/equipe interna/);
    expect(getToken()).toBeNull();
  });

  it('não consulta a API com token vazio', async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    await expect(validarSessao('')).rejects.toThrow(/Informe/);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('rejeita uma identidade cuja validade já terminou', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ ...identidade, expiraEm: identidade.servidorEm })));
    await expect(validarSessao('expirado')).rejects.toThrow(/expirado/);
  });
});

describe('invalidação da sessão', () => {
  it('401 encerra a sessão uma única vez mesmo com requisições paralelas e corpo não JSON', async () => {
    setToken('ativo');
    const ouvinte = vi.fn();
    const remover = onSessaoInvalida(ouvinte);
    try {
      vi.stubGlobal('fetch', vi.fn().mockImplementation(() => Promise.resolve(new Response('Unauthorized', { status: 401 }))));
      const resultados = await Promise.allSettled([request('/chamados'), request('/usuarios')]);
      expect(resultados.every((r) => r.status === 'rejected' && r.reason.status === 401)).toBe(true);
      expect(getToken()).toBeNull();
      expect(ouvinte).toHaveBeenCalledTimes(1);
    } finally { remover(); }
  });

  it.each([403, 500, 503])('HTTP %s mantém a sessão mesmo com corpo HTML', async (status) => {
    setToken('ativo');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html>erro</html>', { status })));
    await expect(request('/chamados')).rejects.toMatchObject({ status });
    expect(getToken()).toBe('ativo');
  });

  it('erro de rede mantém a sessão', async () => {
    setToken('ativo');
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    await expect(request('/chamados')).rejects.toMatchObject({ status: 0 });
    expect(getToken()).toBe('ativo');
  });

  it('401 de uma sessão anterior não encerra um novo login com o mesmo token', async () => {
    setToken('mesmo-token');
    let responder!: (res: Response) => void;
    vi.stubGlobal('fetch', vi.fn(() => new Promise<Response>((resolve) => { responder = resolve; })));
    const antiga = request('/chamados');
    setToken('mesmo-token');
    responder(new Response(null, { status: 401 }));
    await expect(antiga).rejects.toMatchObject({ status: 401 });
    expect(getToken()).toBe('mesmo-token');
  });

  it('validar candidato rejeitado não encerra uma sessão já ativa', async () => {
    setToken('ativo');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 401 })));
    await expect(validarSessao('outro')).rejects.toMatchObject({ status: 401 });
    expect(getToken()).toBe('ativo');
  });
});

describe('expiração', () => {
  it('usa a diferença entre horários da API em vez do relógio local', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2035-01-01'));
    expect(tempoRestanteSessao(identidade)).toBe(3_600_000);
  });

  it('expira sem depender de uma próxima requisição', () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'performance'] });
    const expirar = vi.fn();
    const cancelar = agendarExpiracao(1000, expirar);
    vi.advanceTimersByTime(999);
    expect(expirar).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(expirar).toHaveBeenCalledOnce();
    cancelar();
  });

  it('cancela o timer no logout e suporta tokens de longa duração', () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'performance'] });
    const expirar = vi.fn();
    const cancelar = agendarExpiracao(3_000_000_000, expirar);
    vi.advanceTimersByTime(2_147_483_647);
    expect(expirar).not.toHaveBeenCalled();
    cancelar();
    vi.advanceTimersByTime(3_000_000_000);
    expect(expirar).not.toHaveBeenCalled();
  });
});
