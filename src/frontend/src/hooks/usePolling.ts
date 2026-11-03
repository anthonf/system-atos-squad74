import { useEffect, useRef } from 'react';

export function usePolling(fn: () => void, ms: number, ativo = true) {
  const ref = useRef(fn);
  ref.current = fn;
  useEffect(() => {
    if (!ativo) return;
    const id = setInterval(() => ref.current(), ms);
    return () => clearInterval(id);
  }, [ms, ativo]);
}
