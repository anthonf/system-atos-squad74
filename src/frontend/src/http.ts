import i18n from './i18n';

export const API_URL = ((import.meta.env.VITE_API_URL as string | undefined) || 'https://localhost:7099').replace(/\/$/, '');
export class ApiError extends Error {
  constructor(message: string, readonly status: number, readonly errors: string[] = [], readonly campo?: string) {
    super(message);
    this.name = 'ApiError';
  }
}
export async function send(path: string, options: RequestInit): Promise<Response> {
  try { return await fetch(`${API_URL}${path}`, options); }
  catch { throw new ApiError(i18n.t('api.network'), 0); }
}
export async function responseError(response: Response): Promise<ApiError> {
  let problem: { detail?: string; errors?: string[] | Record<string, string[]>; campo?: string } = {};
  try { problem = await response.json(); } catch { /* Corpo vazio. */ }
  const errors = Array.isArray(problem.errors) ? problem.errors : Object.values(problem.errors || {}).flat();
  const fallback = response.status === 401 ? i18n.t('api.unauthorized')
    : response.status === 403 ? i18n.t('api.forbidden') : i18n.t('api.generic');
  return new ApiError(problem.detail || errors[0] || fallback, response.status, errors, problem.campo);
}
