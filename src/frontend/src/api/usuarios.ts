import { request } from './client';
import type { UsuarioDto } from '../types/usuario';

export function obterUsuario(id: number) {
  return request<UsuarioDto>(`/usuarios/${id}`);
}

export function listarUsuariosPorGrupo(grupoEmpresaId: number) {
  return request<UsuarioDto[]>(`/usuarios?grupoEmpresaId=${grupoEmpresaId}`);
}
