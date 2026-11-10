// Notificações da aplicação: sonner (toast.success / toast.error / toast.warning). Não usar Snackbar do MUI.
import { toast } from 'sonner';
import { ApiError } from './api';

/** Erro de API -> toast. Sessão/permissão (401/403) são avisos; o resto é erro. */
export function notifyApiError(err: unknown, fallback: string): void {
  const message = err instanceof Error && err.message ? err.message : fallback;
  if (err instanceof ApiError && (err.status === 401 || err.status === 403)) toast.warning(message);
  else toast.error(message);
}
