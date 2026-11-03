import { useEffect } from 'react';

// Atalhos da fila: "/" foca a busca, "r" atualiza.
export function useAtalhosFila(opts: { onBuscar: () => void; onAtualizar: () => void }) {
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      const alvo = e.target as HTMLElement;
      const digitando = ['INPUT', 'TEXTAREA', 'SELECT'].includes(alvo.tagName);
      if (e.key === '/' && !digitando) {
        e.preventDefault();
        opts.onBuscar();
      } else if ((e.key === 'r' || e.key === 'R') && !digitando) {
        opts.onAtualizar();
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [opts]);
}
