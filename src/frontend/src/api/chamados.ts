import { request, buildQuery } from './client';
import type {
  ChamadoDetalheDto,
  ChamadoDto,
  FilaQuery,
  InteracaoDto,
  StatusChamado,
  TipoInteracao,
} from '../types/chamado';

export function listarChamados(f: FilaQuery = {}) {
  const q = buildQuery({
    status: f.status,
    categoria: f.categoria,
    produto: f.produto,
    agenteId: f.agenteId,
    grupoEmpresaId: f.grupoEmpresaId,
    semAgente: f.semAgente,
  });
  return request<ChamadoDto[]>(`/chamados${q}`);
}

export function obterChamado(codigo: string) {
  return request<ChamadoDetalheDto>(`/chamados/${encodeURIComponent(codigo)}`);
}

export function responderChamado(codigo: string, mensagem: string, tipo: TipoInteracao = 'Publica') {
  return request<InteracaoDto>(`/chamados/${encodeURIComponent(codigo)}/interacoes`, {
    method: 'POST',
    body: JSON.stringify({ mensagem, tipo }),
  });
}

export function mudarStatus(codigo: string, novoStatus: StatusChamado) {
  return request<ChamadoDto>(`/chamados/${encodeURIComponent(codigo)}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ novoStatus }),
  });
}

export function atribuirAgente(codigo: string, agenteId?: number, automatico = false) {
  return request<ChamadoDto>(`/chamados/${encodeURIComponent(codigo)}/atribuir`, {
    method: 'PATCH',
    body: JSON.stringify({ agenteId: agenteId ?? null, automatico }),
  });
}
