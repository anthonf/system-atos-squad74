// Módulo central de comunicação com a API REST do backend .NET.
// Em dev usa o proxy do Vite (/api). Em produção aponta direto para a API no Render.
const API_BASE = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL.replace(/\/$/, '')}/api`
  : 'https://system-atos-squad74.onrender.com/api';

let _token = null;

export const setToken = (t) => {
  _token = t && t.startsWith('Bearer ') ? t.slice(7) : t;
};
export const getToken = () => _token;
export const clearToken = () => { _token = null; };

async function request(path, options = {}) {
  const headers = { 'Content-Type': 'application/json', ...options.headers };
  if (_token) headers['Authorization'] = `Bearer ${_token}`;

  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 15000);

  try {
    const res = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers,
      signal: controller.signal,
    });
    clearTimeout(timeout);

    if (res.status === 401) {
      clearToken();
      throw new Error('Sessão expirada. Faça login novamente.');
    }
    if (!res.ok) {
      const body = await res.json().catch(() => null);
      throw new Error(body?.detail || body?.title || `Erro ${res.status}`);
    }
    if (res.status === 204) return null;
    return res.json();
  } catch (err) {
    clearTimeout(timeout);
    if (err.name === 'AbortError') throw new Error('Tempo limite excedido. Verifique se o backend está rodando.');
    throw err;
  }
}

// --- Sessão / Usuários ---
export const getSessao = () => request('/usuarios/me');
export const getUsuario = (id) => request(`/usuarios/${id}`);
export const listarUsuariosPorGrupo = (grupoId) => request(`/usuarios?grupoEmpresaId=${grupoId}`);

// --- Chamados ---
export const listarChamados = (filtros = {}) => {
  const params = new URLSearchParams();
  if (filtros.status !== undefined && filtros.status !== null) params.append('status', filtros.status);
  if (filtros.categoria) params.append('categoria', filtros.categoria);
  if (filtros.produto) params.append('produto', filtros.produto);
  if (filtros.agenteId) params.append('agenteId', filtros.agenteId);
  if (filtros.grupoEmpresaId) params.append('grupoEmpresaId', filtros.grupoEmpresaId);
  if (filtros.semAgente) params.append('semAgente', 'true');
  const qs = params.toString();
  return request(`/chamados${qs ? '?' + qs : ''}`);
};

export const consultarChamado = (codigoPublico) => request(`/chamados/${codigoPublico}`);

export const criarChamado = (input) => request('/chamados', {
  method: 'POST',
  body: JSON.stringify(input),
});

export const responderChamado = (codigoPublico, mensagem, tipo = 0) => request(`/chamados/${codigoPublico}/interacoes`, {
  method: 'POST',
  body: JSON.stringify({ mensagem, tipo }),
});

export const mudarStatus = (codigoPublico, novoStatus) => request(`/chamados/${codigoPublico}/status`, {
  method: 'PATCH',
  body: JSON.stringify({ novoStatus }),
});

export const atribuirAgente = (codigoPublico, agenteId = null, automatico = false) => request(`/chamados/${codigoPublico}/atribuir`, {
  method: 'PATCH',
  body: JSON.stringify({ agenteId, automatico }),
});
