import { request } from '../api/client';
import type { SessaoUsuario } from '../types/usuario';

export async function validarSessao(token: string): Promise<SessaoUsuario> {
  if (!token) throw new Error('Informe o token JWT.');
  const sessao = await request<SessaoUsuario>('/usuarios/me', { cache: 'no-store' }, { token });
  if (!['Agente', 'Supervisor', 'Admin'].includes(sessao.papel)) {
    throw new Error('Este painel é para a equipe interna (Agente/Supervisor/Admin).');
  }
  if (tempoRestanteSessao(sessao) <= 0) throw new Error('Token expirado.');
  return sessao;
}

export function tempoRestanteSessao(sessao: SessaoUsuario): number {
  // Usa o relógio da API para não encerrar prematuramente por diferença no navegador.
  const restante = Date.parse(sessao.expiraEm) - Date.parse(sessao.servidorEm);
  if (!Number.isFinite(restante)) throw new Error('A API retornou uma validade de sessão inválida.');
  return restante;
}

export function agendarExpiracao(duracao: number, expirar: () => void): () => void {
  // Evita overflow de setTimeout para tokens de longa duração.
  const inicio = performance.now();
  let timer: ReturnType<typeof setTimeout>;
  function verificar() {
    const restante = duracao - (performance.now() - inicio);
    if (restante <= 0) expirar();
    else timer = setTimeout(verificar, Math.min(restante, 2_147_483_647));
  }
  verificar();
  return () => clearTimeout(timer);
}
