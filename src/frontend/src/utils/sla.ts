export function slaEstourado(slaPrazo: string, status: string): boolean {
  if (status === 'Fechado' || status === 'Resolvido') return false;
  return new Date(slaPrazo).getTime() < Date.now();
}

export function tempoRestante(slaPrazo: string): string {
  const diff = new Date(slaPrazo).getTime() - Date.now();
  const abs = Math.abs(diff);
  const h = Math.floor(abs / 3_600_000);
  const m = Math.floor((abs % 3_600_000) / 60_000);
  const fmt = `${String(h).padStart(2, '0')}h ${String(m).padStart(2, '0')}m`;
  return diff < 0 ? `estourado há ${fmt}` : `faltam ${fmt}`;
}

export function formatarData(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}
