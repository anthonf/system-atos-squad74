// Cliente HTTP autenticado. JWT em memória; cookie HttpOnly apenas nos endpoints de autenticação.
import i18n from './i18n';
import { ApiError, responseError, send } from './http';
import { ensureAccessToken, expireSession, getAuthEpoch, getTokenVersion, refreshSession } from './auth';
import { parseApiDate } from './dates';
import type { PriorityKey, Session, StatusKey, Ticket, TipoInteracaoApi } from './types';

export const TITULO_MAX = 200;

/* ---------------------------------------------------------------------- */
/* Contratos da API (espelham os DTOs do backend)                          */
/* ---------------------------------------------------------------------- */

export type PapelApi = 'Cliente' | 'Agente' | 'Supervisor' | 'Admin';
/** Papéis de funcionários internos: podem ser cadastrados sem grupo de empresa e recebem convite de ativação. */
export type PapelInterno = Exclude<PapelApi, 'Cliente'>;
export type StatusApi = 'Aberto' | 'EmAndamento' | 'AguardandoCliente' | 'Resolvido' | 'Fechado';

export interface SlaCategoriaDto {
  produto: string;
  categoria: string;
  prioridade: string;
}

export interface ChamadoDto {
  id: number;
  codigoPublico: string;
  usuarioId: number;
  grupoEmpresaId: number;
  agenteId: number | null;
  produto: string;
  categoria: string;
  titulo: string;
  descricao: string;
  status: StatusApi;
  prioridade: string;
  slaPrazo: string;
  criadoEm: string;
  slaPrazoResposta?: string | null;
  aguardandoDesde?: string | null;
  atualizadoEm?: string | null;
  fechadoEm?: string | null;
  usuarioNome?: string | null;
  grupoEmpresaNome?: string | null;
  agenteNome?: string | null;
}

export interface InteracaoDto {
  id: number;
  usuarioId: number;
  tipo: TipoInteracaoApi;
  mensagem: string;
  criadoEm: string;
  anexos?: unknown[];
  usuarioNome?: string | null;
  papelUsuario?: PapelApi | null;
}

export interface ChamadoDetalheDto {
  chamado: ChamadoDto;
  interacoes: InteracaoDto[];
}

export interface AgenteDto {
  id: number;
  nome: string;
  papel: PapelInterno;
}

export interface FiltrosChamados {
  status?: StatusApi;
  categoria?: string;
  produto?: string;
  agenteId?: number;
  grupoEmpresaId?: number;
  semAgente?: boolean;
}

export interface AtribuirChamadoInput {
  agenteId?: number;
  automatico?: boolean;
  desatribuir?: boolean;
}

export interface NovoChamadoInput {
  titulo: string;
  produto: string;
  categoria: string;
  descricao: string;
}

export interface UsuarioDto {
  id: number;
  nome: string;
  email: string;
  grupoEmpresaId: number | null;
  grupoEmpresaNome: string | null;
  papel: PapelApi;
}

/** Resposta de POST /api/usuarios para Admin, Supervisor e Agente (convite de ativação). */
export interface ConviteFuncionarioDto extends UsuarioDto {
  papel: PapelInterno;
  tokenAtivacao: string;
  expiraEm: string;
}

/** Distingue a resposta de convite (funcionário interno) da resposta de cliente. */
export function isConviteFuncionario(
  usuario: UsuarioDto | ConviteFuncionarioDto,
): usuario is ConviteFuncionarioDto {
  return usuario !== null && typeof usuario === 'object' && 'tokenAtivacao' in usuario
    && typeof usuario.tokenAtivacao === 'string' && usuario.tokenAtivacao.trim().length > 0
    && Number.isInteger(usuario.id) && usuario.id > 0
    && typeof usuario.nome === 'string' && usuario.nome.trim().length > 0
    && 'expiraEm' in usuario && typeof usuario.expiraEm === 'string'
    && Number.isFinite(Date.parse(usuario.expiraEm))
    && ['Admin', 'Supervisor', 'Agente'].includes(usuario.papel);
}

export interface NovoUsuarioInput {
  nome: string;
  email: string;
  /** Obrigatório apenas para Cliente; funcionários internos enviam null. */
  grupoEmpresaId: number | null;
  papel: PapelApi;
}

/* ---------------------------------------------------------------------- */
/* Transporte                                                              */
/* ---------------------------------------------------------------------- */

export { ApiError } from './http';

async function request<T>(path: string, { method = 'GET', body }: { method?: string; body?: unknown } = {}): Promise<T> {
  let token = await ensureAccessToken();
  const epoch = getAuthEpoch();
  const observedVersion = getTokenVersion();
  for (let attempt = 0; attempt < 2; attempt++) {
    if (epoch !== getAuthEpoch()) throw new ApiError(i18n.t('api.unauthorized'), 401);
    const response = await send(path, {
      method,
      headers: { Accept: 'application/json', Authorization: `Bearer ${token}`,
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }) },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });
    if (epoch !== getAuthEpoch()) throw new ApiError(i18n.t('api.unauthorized'), 401);
    if (response.status === 401 && attempt === 0) {
      await refreshSession(observedVersion);
      if (epoch !== getAuthEpoch()) throw new ApiError(i18n.t('api.unauthorized'), 401);
      token = await ensureAccessToken();
      continue;
    }
    if (response.status === 401) expireSession(epoch);
    if (!response.ok) throw await responseError(response);
    const payload = response.status === 204 ? null : await response.json();
    if (epoch !== getAuthEpoch()) throw new ApiError(i18n.t('api.unauthorized'), 401);
    return payload as T;
  }
  throw new ApiError(i18n.t('api.unauthorized'), 401);
}

/* ---------------------------------------------------------------------- */
/* Chamados — equipe (JWT interno)                                         */
/* ---------------------------------------------------------------------- */

export const listarChamados = (filtros: FiltrosChamados = {}) => {
  const params = new URLSearchParams();
  Object.entries(filtros).forEach(([chave, valor]) => {
    if (valor !== undefined && valor !== null) params.append(chave, String(valor));
  });
  const query = params.toString();
  return request<ChamadoDto[]>(`/api/chamados${query ? `?${query}` : ''}`);
};

export const consultarChamado = (codigoPublico: string) =>
  request<ChamadoDetalheDto>(`/api/chamados/${encodeURIComponent(codigoPublico)}`);

export const responderChamado = (
  codigoPublico: string,
  mensagem: string,
  tipo: TipoInteracaoApi = 'Publica',
) => request<InteracaoDto>(`/api/chamados/${encodeURIComponent(codigoPublico)}/interacoes`, {
  method: 'POST',
  body: { mensagem, tipo },
});

export const mudarStatusChamado = (codigoPublico: string, novoStatus: StatusApi) =>
  request<ChamadoDto>(`/api/chamados/${encodeURIComponent(codigoPublico)}/status`, {
    method: 'PATCH',
    body: { novoStatus },
  });

export const atribuirChamado = (codigoPublico: string, input: AtribuirChamadoInput) =>
  request<ChamadoDto>(`/api/chamados/${encodeURIComponent(codigoPublico)}/atribuir`, {
    method: 'PATCH',
    body: input,
  });

export const listarAgentes = () => request<AgenteDto[]>('/api/usuarios/agentes');

/** Opções reais de produto/categoria (tabela sla_categoria). */
export const listarSlaCategorias = () => request<SlaCategoriaDto[]>('/api/sla-categorias');

/** Abre um chamado com a identidade da equipe autenticada (POST /api/chamados/interno). */
export const criarChamado = ({ titulo, produto, categoria, descricao }: NovoChamadoInput) =>
  request<ChamadoDto>('/api/chamados/interno', {
    method: 'POST',
    body: { titulo: titulo.trim(), produto, categoria, mensagemInicial: descricao.trim() },
  });

/* ---------------------------------------------------------------------- */
/* Administração de usuários (perfil Admin)                                */
/* ---------------------------------------------------------------------- */

export const listarUsuariosDoGrupo = (grupoEmpresaId: number) =>
  request<UsuarioDto[]>(`/api/usuarios?grupoEmpresaId=${encodeURIComponent(grupoEmpresaId)}`);

export interface FuncionarioListaDto {
  id: number;
  nome: string;
  email: string;
  papel: PapelApi;
  situacao: 'Pendente' | 'Ativa' | 'Inativa';
}

/** Lista todos os funcionários internos (Admin, Supervisor, Agente) com situação da credencial. */
export const listarFuncionarios = () => request<FuncionarioListaDto[]>('/api/usuarios/funcionarios');

/** Cliente retorna UsuarioDto; funcionário interno retorna ConviteFuncionarioDto. */
export const cadastrarUsuario = ({ nome, email, grupoEmpresaId, papel }: NovoUsuarioInput) =>
  request<UsuarioDto | ConviteFuncionarioDto>('/api/usuarios', {
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
export const STATUS_API: Record<StatusKey, StatusApi> = {
  aberto: 'Aberto', em_andamento: 'EmAndamento', aguardando_cliente: 'AguardandoCliente', resolvido: 'Resolvido', fechado: 'Fechado',
};

export const normalizar = (v: string) => v.normalize('NFD').replace(/[\u0300-\u036f_\s]/g, '').toLowerCase();
export const statusDtoParaKey = (status: string): StatusKey => STATUS[normalizar(status)] ?? 'aberto';

/** ChamadoDto (e, opcionalmente, as interações do detalhe) -> formato usado pelas telas. */
export function chamadoDtoParaTicket(dto: ChamadoDto, user: Session, interacoes: InteracaoDto[] = []): Ticket {
  const prazo = parseApiDate(dto.slaPrazo);
  const criado = parseApiDate(dto.criadoEm);
  const total = Math.max(1, Math.round((prazo.valueOf() - criado.valueOf()) / HORA));
  const restante = Math.max(0, Math.min(total, Math.round((prazo.valueOf() - Date.now()) / HORA)));
  const abertura = criado.toISOString();
  const autorPadrao = dto.usuarioNome || user.nome;
  const mensagens = interacoes
    .filter((i) => i.tipo === 'Publica')
    .map((i) => ({
      autor: i.usuarioNome || (i.usuarioId === dto.usuarioId ? autorPadrao : user.nome),
      autorId: i.usuarioId,
      tipo: i.papelUsuario && i.papelUsuario !== 'Cliente' ? 'agente' as const : 'cliente' as const,
      tipoInteracao: 'Publica' as const,
      data: parseApiDate(i.criadoEm).toISOString(),
      texto: i.mensagem,
    }));
  if (mensagens.length === 0 && dto.descricao) {
    mensagens.push({
      autor: autorPadrao,
      autorId: dto.usuarioId,
      tipo: 'cliente',
      tipoInteracao: 'Publica',
      data: abertura,
      texto: dto.descricao,
    });
  }
  const notas = interacoes
    .filter((i) => i.tipo === 'NotaInterna')
    .map((i) => ({
      autor: i.usuarioNome || user.nome,
      autorId: i.usuarioId,
      tipoInteracao: 'NotaInterna' as const,
      data: parseApiDate(i.criadoEm).toISOString(),
      texto: i.mensagem,
    }));

  return {
    id: dto.codigoPublico,
    usuarioId: dto.usuarioId,
    grupoEmpresaId: dto.grupoEmpresaId,
    agenteId: dto.agenteId,
    empresa: dto.grupoEmpresaNome || user.empresa || '—',
    contato: dto.usuarioNome || user.nome,
    produto: dto.produto,
    categoria: dto.categoria,
    assunto: dto.titulo,
    prioridade: PRIORIDADE[normalizar(dto.prioridade)] ?? 'media',
    status: statusDtoParaKey(dto.status),
    agente: dto.agenteNome ?? null,
    slaTotal: total,
    slaRestante: restante,
    abertura,
    aguardandoDesde: dto.aguardandoDesde ? parseApiDate(dto.aguardandoDesde).toISOString() : undefined,
    prazoAutoEncerramento: dto.aguardandoDesde ? parseApiDate(dto.slaPrazo).toISOString() : undefined,
    mensagens,
    notas,
  };
}
