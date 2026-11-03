export type PapelUsuario = 'Cliente' | 'Agente' | 'Supervisor' | 'Admin';

export interface UsuarioDto {
  id: number;
  nome: string;
  email: string;
  grupoEmpresaId: number;
  grupoEmpresaNome: string | null;
  papel: PapelUsuario;
}

export interface SessaoUsuario {
  usuarioId: number;
  email: string;
  nome: string;
  grupoEmpresaId: number;
  grupoEmpresaNome: string;
  papel: PapelUsuario;
  expiraEm: string;
  servidorEm: string;
}
