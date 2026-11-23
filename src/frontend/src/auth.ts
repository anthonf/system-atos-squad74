import { ApiError, responseError, send } from './http';
import type { Session } from './types';

// Remove apenas o token legado do login simulado; a sessão nova nunca usa Storage.
try { window.sessionStorage.removeItem('atos_token'); window.localStorage.removeItem('atos_token'); } catch { /* Storage bloqueado. */ }

export interface AuthResult {
  token: string;
  expiraEm: string;
  usuarioId: number;
  nome: string;
  email: string;
  papel: 'Admin' | 'Supervisor' | 'Agente';
  grupoEmpresaId: number | null;
}
type AuthState = { status: 'restoring' | 'authenticated' | 'anonymous'; session: Session | null };
let state: AuthState = { status: 'restoring', session: null };
let token = '';
let expiresAt = 0;
let epoch = 0;
let version = 0;
let restored = false;
let refreshFlight: Promise<Session> | null = null;
let restoreFlight: Promise<Session | null> | null = null;
const listeners = new Set<() => void>();
// Serializa mutações do cookie para que um refresh atrasado não ressuscite um logout.
let authTail: Promise<unknown> = Promise.resolve();
function serialize<T>(operation: () => Promise<T>): Promise<T> {
  const result = authTail.then(operation);
  authTail = result.catch(() => undefined);
  return result;
}
function publish(status: AuthState['status'], session: Session | null = null) {
  state = { status, session };
  listeners.forEach(listener => listener());
}
export const getAuthState = () => state;
export const subscribeAuth = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener); }; };
export const getToken = () => token;
export const getAuthEpoch = () => epoch;
export const getTokenVersion = () => version;
const unauthorized = () => new ApiError('Sessão encerrada. Entre novamente.', 401);

export function expireSession(expectedEpoch = epoch) {
  if (expectedEpoch !== epoch) return;
  epoch++;
  token = '';
  expiresAt = 0;
  restored = true;
  publish('anonymous');
}
function accept(result: AuthResult, expectedEpoch: number): Session {
  if (expectedEpoch !== epoch) throw unauthorized();
  const roles = { Admin: 'admin', Supervisor: 'supervisor', Agente: 'agente' } as const;
  const expiry = Date.parse(result?.expiraEm);
  if (!result || typeof result.token !== 'string' || !result.token || !Object.prototype.hasOwnProperty.call(roles, result.papel)
    || !Number.isFinite(expiry) || expiry <= Date.now() || !Number.isInteger(result.usuarioId) || result.usuarioId <= 0
    || typeof result.nome !== 'string' || !result.nome || typeof result.email !== 'string' || !result.email
    || (result.grupoEmpresaId !== null && !Number.isInteger(result.grupoEmpresaId))) {
    throw new ApiError('Resposta de autenticação inválida.', 401);
  }
  const session: Session = {
    usuarioId: result.usuarioId, grupoEmpresaId: result.grupoEmpresaId,
    nome: result.nome, email: result.email, role: roles[result.papel], empresa: null,
  };
  token = result.token;
  expiresAt = expiry;
  version++;
  publish('authenticated', session);
  return session;
}
async function authRequest(path: string, body?: unknown): Promise<Response> {
  const response = await send(`/api/auth/${path}`, {
    method: 'POST', credentials: 'include',
    ...(path === 'ativar' ? { cache: 'no-store' as const, referrerPolicy: 'no-referrer' as const } : {}),
    headers: { Accept: 'application/json', ...(body === undefined ? {} : { 'Content-Type': 'application/json' }) },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
  if (!response.ok) throw await responseError(response);
  return response;
}

/** Ativa um funcionário pelo convite de uso único. Reutiliza o mesmo transporte/CSRF do login. */
export async function ativarFuncionario({ token, senha }: { token: string; senha: string }): Promise<void> {
  await authRequest('ativar', { token, senha });
}
export function refreshSession(observedVersion = version): Promise<Session> {
  if (state.status === 'anonymous') return Promise.reject(unauthorized());
  if (observedVersion !== version && state.session) return Promise.resolve(state.session);
  if (refreshFlight) return refreshFlight;
  const startedEpoch = epoch;
  const flight = serialize(async () => {
    if (epoch !== startedEpoch) throw unauthorized();
    const response = await authRequest('refresh');
    return accept(await response.json() as AuthResult, startedEpoch);
  }).catch(error => { expireSession(startedEpoch); throw error; });
  refreshFlight = flight;
  void flight.finally(() => { if (refreshFlight === flight) refreshFlight = null; }).catch(() => undefined);
  return flight;
}
export function restoreSession(): Promise<Session | null> {
  if (restored) return restoreFlight || Promise.resolve(state.session);
  restored = true;
  restoreFlight = refreshSession().catch(() => null);
  void restoreFlight.finally(() => { restoreFlight = null; });
  return restoreFlight;
}
export async function ensureAccessToken(): Promise<string> {
  if (state.status === 'restoring') await restoreSession();
  if (state.status !== 'authenticated') throw unauthorized();
  if (!token || expiresAt <= Date.now() + 5000) await refreshSession();
  return token;
}
export async function login(email: string, senha: string): Promise<Session> {
  const startedEpoch = ++epoch;
  restored = true;
  token = '';
  expiresAt = 0;
  publish('anonymous');
  try {
    return await serialize(async () => {
      const response = await authRequest('login', { email: email.trim(), senha });
      return accept(await response.json() as AuthResult, startedEpoch);
    });
  } catch (error) { expireSession(startedEpoch); throw error; }
}
export async function logout(): Promise<void> {
  expireSession();
  await serialize(async () => { await authRequest('logout'); });
}
