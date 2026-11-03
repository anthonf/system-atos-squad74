// Converte entre os formatos da API (enums numéricos) e do frontend (strings).

const STATUS_MAP = { 0: 'aberto', 1: 'em_andamento', 2: 'aguardando_cliente', 3: 'resolvido', 4: 'fechado' };
const STATUS_REVERSE = { aberto: 0, em_andamento: 1, aguardando_cliente: 2, resolvido: 3, fechado: 4 };
const PRIORIDADE_MAP = { 0: 'baixa', 1: 'media', 2: 'alta', 3: 'urgente' };
const PAPEL_MAP = { 0: 'cliente', 1: 'agente', 2: 'supervisor', 3: 'admin' };

export const mapStatus = (n) => STATUS_MAP[n] ?? 'aberto';
export const reverseStatus = (s) => STATUS_REVERSE[s] ?? 0;
export const mapPrioridade = (n) => PRIORIDADE_MAP[n] ?? 'baixa';
export const mapPapel = (n) => PAPEL_MAP[n] ?? 'cliente';

// Formata data ISO → "dd/mm HH:mm"
function fmtData(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })
    + ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

// Converte ChamadoDto da API → formato que os componentes esperam
export function mapChamadoFromApi(dto) {
  const slaPrazo = new Date(dto.slaPrazo);
  const agora = new Date();
  const slaRestanteMs = slaPrazo - agora;
  const slaRestanteH = Math.max(0, Math.round(slaRestanteMs / (1000 * 60 * 60)));
  const criadoEm = new Date(dto.criadoEm);

  return {
    id: dto.codigoPublico,
    _apiId: dto.id,
    empresa: '',
    contato: '',
    produto: dto.produto || '',
    categoria: dto.categoria || '',
    titulo: dto.titulo || '',
    descricao: dto.descricao || '',
    prioridade: mapPrioridade(dto.prioridade),
    status: mapStatus(dto.status),
    agente: null, // preenchido depois se possível
    agenteId: dto.agenteId,
    usuarioId: dto.usuarioId,
    grupoEmpresaId: dto.grupoEmpresaId,
    slaTotal: Math.max(1, Math.round((slaPrazo - criadoEm) / (1000 * 60 * 60))),
    slaRestante: slaRestanteH,
    slaPrazo: dto.slaPrazo,
    abertura: fmtData(dto.criadoEm),
    aguardandoDesde: dto.aguardandoDesde ? fmtData(dto.aguardandoDesde) : null,
    mensagens: [],
    notas: [],
  };
}

// Converte ChamadoDetalheDto (chamado + interações) → formato do frontend
export function mapDetalheFromApi(detalheDto) {
  const chamado = mapChamadoFromApi(detalheDto.chamado);
  const mensagens = [];
  const notas = [];

  for (const inter of detalheDto.interacoes || []) {
    const isNota = inter.tipo === 1;
    const isCliente = inter.usuarioId === detalheDto.chamado.usuarioId;
    const item = {
      id: inter.id,
      autor: `Usuário #${inter.usuarioId}`,
      tipo: isNota ? 'nota' : (isCliente ? 'cliente' : 'agente'),
      data: fmtData(inter.criadoEm),
      texto: inter.mensagem,
    };
    if (isNota) {
      notas.push({ autor: item.autor, data: item.data, texto: item.texto });
    } else {
      mensagens.push(item);
    }
  }

  chamado.mensagens = mensagens;
  chamado.notas = notas;
  return chamado;
}

// Converte SessaoUsuarioDto → formato de sessão do frontend
export function mapSessaoFromApi(dto) {
  const papel = mapPapel(dto.papel);
  return {
    nome: dto.nome,
    email: dto.email,
    papel: papel,
    role: papel === 'cliente' ? 'cliente' : 'agente',
    usuarioId: dto.usuarioId,
    grupoEmpresaId: dto.grupoEmpresaId,
    empresa: dto.grupoEmpresaNome || '',
    expiraEm: dto.expiraEm,
  };
}
