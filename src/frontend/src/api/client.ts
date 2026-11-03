import { ApiError, mensagemAmigavel } from './errors';

const BASE_URL = import.meta.env.VITE_API_URL || '/api';

let tokenEmMemoria: string | null = null;
let versaoSessao = 0;
const ouvintesSessaoInvalida = new Set<() => void>();

export function setToken(t: string | null) {
  tokenEmMemoria = t;
  versaoSessao++;
}
export function getToken(): string | null {
  return tokenEmMemoria;
}

export function onSessaoInvalida(ouvinte: () => void): () => void {
  ouvintesSessaoInvalida.add(ouvinte);
  return () => { ouvintesSessaoInvalida.delete(ouvinte); };
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: string[] | Record<string, string[]>;
}

export async function request<T>(path: string, init: RequestInit = {}, autenticacao?: { token: string }): Promise<T> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 15000);
  const versaoRequisicao = versaoSessao;
  const token = autenticacao ? autenticacao.token : getToken();
  try {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(init.headers as Record<string, string> | undefined),
    };
    if (token) headers.Authorization = `Bearer ${token}`;

    const res = await fetch(`${BASE_URL}${path}`, {
      ...init,
      headers,
      signal: controller.signal,
    });

    if (res.status === 401 && !autenticacao && token && versaoRequisicao === versaoSessao) {
      setToken(null);
      ouvintesSessaoInvalida.forEach((ouvinte) => ouvinte());
    }

    if (res.status === 204) return undefined as T;

    const text = await res.text();
    let body: (T & ProblemDetails) | undefined;
    try {
      body = text ? JSON.parse(text) as T & ProblemDetails : undefined;
    } catch {
      // Proxies e respostas de erro podem retornar HTML ou texto simples.
      if (res.ok) throw new ApiError(502, 'Resposta inválida do servidor.');
    }

    if (!res.ok) {
      const b = (body || {}) as ProblemDetails;
      const flatErrors = Array.isArray(b.errors)
        ? b.errors
        : b.errors
          ? Object.values(b.errors).flat()
          : undefined;
      const msg = mensagemAmigavel(res.status, b.detail || b.title || `Erro ${res.status}`);
      throw new ApiError(res.status, msg, b.detail || b.title, flatErrors);
    }
    return body as T;
  } catch (e) {
    if (e instanceof ApiError) throw e;
    if (e instanceof DOMException && e.name === 'AbortError') {
      throw new ApiError(0, 'Tempo esgotado ao falar com o servidor.');
    }
    throw new ApiError(0, 'Não foi possível conectar ao servidor.');
  } finally {
    clearTimeout(timeout);
  }
}

export function buildQuery(params: Record<string, string | number | boolean | undefined>): string {
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== '') q.append(k, String(v));
  }
  const s = q.toString();
  return s ? `?${s}` : '';
}
