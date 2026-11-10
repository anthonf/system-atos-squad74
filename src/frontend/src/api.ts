// Cliente HTTP da API SupportAtos (backend/src/SupportAtos.Api). Sem dados mockados.
//
// Configuração (arquivo .env.local na raiz do front):
//   VITE_API_URL=http://localhost:5177     # URL base da API (padrão abaixo)
//   VITE_API_TOKEN=<jwt>                   # opcional, só para desenvolvimento; o token vem do SSO
// Em runtime o token pode ficar em sessionStorage na chave "atos_token" (o login SSO deve gravá-lo ali).
import i18n from './i18n';
import { parseApiDate } from './dates';
import type { PriorityKey, Session, StatusKey, Ticket } from './types';

const BASE_URL = (import.meta.env.VITE_API_URL || (import.meta.env.PROD ? 'https://system-atos-api.onrender.com' : 'http://localhost:5177')).replace(/\/$/, '');
export const TITULO_MAX = 200;

/* ---------------------------------------------------------------------- */
/* Contratos da API (espelham os DTOs do backend)                          */
/* ---------------------------------------------------------------------- */

export type PapelApi = 'Cliente' | 'Agente' | 'Supervisor' | 'Admin';

export interface SlaCategoriaDto {
  produto: string;
  categoria: string;
  prioridade: string;
}

export interface ChamadoDto {
  id: number;
  codigoPublico: string;
  produto: string;
  categoria: string;
  titulo: string;
  descricao: string;
  status: string;
  prioridade: string;
  slaPrazo: string;
  criadoEm: string;
}

export interface NovoChamadoInput {
  titulo: string;
  produto: string;
  categoria: string;
  descricao: string;
}

export interface GrupoEmpresaDto {
  id: number;
  nome: string;
  tipo: string;
}

export interface UsuarioDto {
  id: number;
  nome: string;
  email: string;
  grupoEmpresaId: number;
  grupoEmpresaNome: string | null;
  papel: PapelApi;
}

export interface NovoUsuarioInput {
  nome: string;
  email: string;
  grupoEmpresaId: number;
  papel: PapelApi;
}

/* ---------------------------------------------------------------------- */
/* Transporte                                                              */
/* ---------------------------------------------------------------------- */

export class ApiError extends Error {
  readonly status: number;
  readonly errors: string[];

  constructor(message: string, status: number, errors: string[] = []) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errors = errors;
  }
}

interface ProblemDetails {
  detail?: string;
  errors?: string[];
}

export function getToken(): string {
  try {
    const stored = window.sessionStorage.getItem('atos_token');
    if (stored) return stored;
  } catch { /* sessionStorage indisponível */ }
  return (import.meta.env.VITE_API_TOKEN as string | undefined) || '';
}

export function setToken(token: string): void {
  try { window.sessionStorage.setItem('atos_token', token); } catch { /* sessionStorage indisponível */ }
}

export function clearToken(): void {
  try { window.sessionStorage.removeItem('atos_token'); } catch { /* sessionStorage indisponível */ }
}

async function request<T>(path: string, { method = 'GET', body }: { method?: string; body?: unknown } = {}): Promise<T> {
  const token = getToken();
  if (!token) {
    throw new ApiError(i18n.t('api.noToken'), 401);
  }

  let response: Response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${token}`,
        ...(body !== undefined && { 'Content-Type': 'application/json' }),
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError(i18n.t('api.network'), 0);
  }

  if (response.ok) return (response.status === 204 ? null : await response.json()) as T;

  // ProblemDetails do backend: { title, detail, errors: [...] }
  let problem: ProblemDetails | null = null;
  try { problem = (await response.json()) as ProblemDetails; } catch { /* corpo vazio */ }
  const errors = Array.isArray(problem?.errors) ? problem.errors : [];
  const fallback = ({
    401: i18n.t('api.unauthorized'),
    403: i18n.t('api.forbidden'),
  } as Record<number, string>)[response.status] || i18n.t('api.generic');
  throw new ApiError(problem?.detail || errors[0] || fallback, response.status, errors);
}

/* ---------------------------------------------------------------------- */
/* Login de desenvolvimento (só existe com a API em Development)           */
/* ---------------------------------------------------------------------- */

export interface DevLoginInput {
  email: string;
  nome: string;
  papel: PapelApi;
  grupoId?: string;
  grupoNome?: string;
}

/**
 * Pede à API (POST /api/dev/token) um JWT de desenvolvimento e o guarda para as próximas chamadas.
 * Em produção esse endpoint não existe: o token vem do SSO e deve ser gravado em sessionStorage ("atos_token").
 */
export async function loginDev(input: DevLoginInput): Promise<void> {
  let response: Response;
  try {
    response = await fetch(`${BASE_URL}/api/dev/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(input),
    });
  } catch {
    throw new ApiError(i18n.t('api.network'), 0);
  }
  if (!response.ok) {
    let detail: string | undefined;
    try { detail = ((await response.json()) as ProblemDetails).detail; } catch { /* corpo vazio */ }
    throw new ApiError(detail || i18n.t('api.generic'), response.status);
  }
  const { token } = (await response.json()) as { token: string };
  setToken(token);
}

/* ---------------------------------------------------------------------- */
/* Chamados                                                                */
/* ---------------------------------------------------------------------- */

/** Opções reais de produto/categoria (tabela sla_categoria). */
export const listarSlaCategorias = () => request<SlaCategoriaDto[]>('/api/sla-categorias');

/** Abre um chamado. `titulo` é obrigatório (validado também no servidor). */
export const criarChamado = ({ titulo, produto, categoria, descricao }: NovoChamadoInput) =>
  request<ChamadoDto>('/api/chamados', {
    method: 'POST',
    body: { titulo: titulo.trim(), produto, categoria, mensagemInicial: descricao.trim() },
  });

/* ---------------------------------------------------------------------- */
/* Administração de usuários (perfil Admin)                                */
/* ---------------------------------------------------------------------- */

export const listarGruposEmpresa = () => request<GrupoEmpresaDto[]>('/api/grupos-empresa');

export const listarUsuariosDoGrupo = (grupoEmpresaId: number) =>
  request<UsuarioDto[]>(`/api/usuarios?grupoEmpresaId=${encodeURIComponent(grupoEmpresaId)}`);

export const cadastrarUsuario = ({ nome, email, grupoEmpresaId, papel }: NovoUsuarioInput) =>
  request<UsuarioDto>('/api/usuarios', {
    method: 'POST',
    body: { nome: nome.trim(), email: email.trim().toLowerCase(), grupoEmpresaId, papel },
  });

/* ---------------------------------------------------------------------- */
/* Mapeamento para o modelo das telas                                      */
/* ---------------------------------------------------------------------- */

const HORA = 3600 * 1000;
const PRIORIDADE: Record<string, PriorityKey> = { alta: 'alta', media: 'media', baixa: 'baixa' };
const STATUS: Record<string, StatusKey> = {
  aberto: 'aberto', emandamento: 'em_andamento', aguardandocliente: 'aguardando_cliente', resolvido: 'resolvido', fechado: 'fechado',
};
const normalizar = (v: string) => v.normalize('NFD').replace(/[\u0300-\u036f_\s]/g, '').toLowerCase();

/** ChamadoDto da API -> formato de chamado usado pelas telas. */
export function chamadoDtoParaTicket(dto: ChamadoDto, user: Session): Ticket {
  const prazo = parseApiDate(dto.slaPrazo);
  const criado = parseApiDate(dto.criadoEm);
  const total = Math.max(1, Math.round((prazo.valueOf() - criado.valueOf()) / HORA));
  const restante = Math.max(0, Math.round((prazo.valueOf() - Date.now()) / HORA));
  return {
    id: dto.codigoPublico,
    empresa: user.empresa ?? '',
    contato: user.nome,
    produto: dto.produto,
    categoria: dto.categoria,
    assunto: dto.titulo,
    prioridade: PRIORIDADE[normalizar(dto.prioridade)] ?? 'media',
    status: STATUS[normalizar(dto.status)] ?? 'aberto',
    agente: null,
    slaTotal: total,
    slaRestante: Math.min(total, restante),
    abertura: criado.toISOString(),
    mensagens: [{ autor: user.nome, tipo: 'cliente', data: criado.toISOString(), texto: dto.descricao }],
    notas: [],
  };
}
