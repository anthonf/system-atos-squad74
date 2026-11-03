import { describe, expect, it } from 'vitest';
import { buildQuery } from '../api/client';
import { mensagemAmigavel } from '../api/errors';
import { mascararEmail } from '../utils/mask';
import { slaEstourado } from '../utils/sla';

describe('camada api (unit)', () => {
  it('buildQuery monta query sem vazios', () => {
    expect(buildQuery({ status: 'Aberto', semAgente: true, produto: '' })).toBe('?status=Aberto&semAgente=true');
    expect(buildQuery({})).toBe('');
  });

  it('mensagemAmigavel cobre 401/403/404/offline', () => {
    expect(mensagemAmigavel(401, '')).toMatch(/Sessão/);
    expect(mensagemAmigavel(403, '')).toMatch(/permissão/);
    expect(mensagemAmigavel(404, '')).toMatch(/não encontrado/i);
    expect(mensagemAmigavel(0, '')).toMatch(/conectar/);
  });

  it('mascararEmail preserva domínio e esconde usuário', () => {
    expect(mascararEmail('agente@atos.com')).toBe('ag***@atos.com');
  });

  it('slaEstourado ignora Resolvido/Fechado', () => {
    const passado = new Date(Date.now() - 3600_000).toISOString();
    expect(slaEstourado(passado, 'Aberto')).toBe(true);
    expect(slaEstourado(passado, 'Fechado')).toBe(false);
  });
});
