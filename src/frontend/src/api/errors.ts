// Erro padronizado da camada API (mapeia ProblemDetails do backend).
export class ApiError extends Error {
  status: number;
  detail?: string;
  errors?: string[];
  constructor(status: number, message: string, detail?: string, errors?: string[]) {
    super(message);
    this.status = status;
    this.detail = detail;
    this.errors = errors;
  }
}

export function mensagemAmigavel(status: number, fallback: string): string {
  if (status === 0) return 'Não foi possível conectar ao servidor. Confira se a API está rodando.';
  if (status === 400) return fallback || 'Dados inválidos.';
  if (status === 401) return 'Sessão expirada ou inválida. Faça login novamente.';
  if (status === 403) return 'Você não tem permissão para esta ação.';
  if (status === 404) return 'Registro não encontrado.';
  if (status === 409) return fallback || 'Conflito: registro duplicado.';
  if (status >= 500) return 'Erro interno do servidor. Tente novamente.';
  return fallback || `Erro na requisição (${status}).`;
}
