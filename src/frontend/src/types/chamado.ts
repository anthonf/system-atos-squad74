// Espelha os DTOs do backend (SupportAtos.Application/DTOs).
// Enums serializados como string via JsonStringEnumConverter.

export type StatusChamado =
  | 'Aberto'
  | 'EmAndamento'
  | 'AguardandoCliente'
  | 'Resolvido'
  | 'Fechado';

export type Prioridade = 'Baixa' | 'Media' | 'Alta' | 'Urgente';

export type TipoInteracao = 'Publica' | 'NotaInterna';

export interface AnexoDto {
  id: number;
  nomeArquivo: string;
  url: string;
  tamanhoBytes: number;
}

export interface InteracaoDto {
  id: number;
  usuarioId: number;
  tipo: TipoInteracao;
  mensagem: string;
  criadoEm: string;
  anexos: AnexoDto[];
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
  status: StatusChamado;
  prioridade: Prioridade;
  slaPrazo: string;
  slaPrazoResposta: string | null;
  aguardandoDesde: string | null;
  criadoEm: string;
  atualizadoEm: string | null;
  fechadoEm: string | null;
}

export interface ChamadoDetalheDto {
  chamado: ChamadoDto;
  interacoes: InteracaoDto[];
}

export interface FilaQuery {
  status?: StatusChamado;
  categoria?: string;
  produto?: string;
  agenteId?: number;
  grupoEmpresaId?: number;
  semAgente?: boolean;
}

export const STATUS_LABEL: Record<StatusChamado, string> = {
  Aberto: 'Aberto',
  EmAndamento: 'Em andamento',
  AguardandoCliente: 'Aguardando cliente',
  Resolvido: 'Resolvido',
  Fechado: 'Fechado',
};

export const PRIORIDADE_LABEL: Record<Prioridade, string> = {
  Baixa: 'Baixa',
  Media: 'Média',
  Alta: 'Alta',
  Urgente: 'Urgente',
};

// Transições válidas (espelham Chamado.MudarStatus no backend)
export const TRANSICOES: Record<StatusChamado, StatusChamado[]> = {
  Aberto: ['EmAndamento', 'AguardandoCliente', 'Resolvido', 'Fechado'],
  EmAndamento: ['AguardandoCliente', 'Resolvido', 'Fechado', 'Aberto'],
  AguardandoCliente: ['EmAndamento', 'Resolvido', 'Fechado'],
  Resolvido: ['Fechado', 'Aberto', 'EmAndamento'],
  Fechado: ['Aberto'],
};
