import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { onSessaoInvalida, setToken } from '../api/client';
import type { PapelUsuario, SessaoUsuario } from '../types/usuario';
import { agendarExpiracao, tempoRestanteSessao, validarSessao } from './session';

interface AuthState {
  token: string | null;
  sessao: SessaoUsuario | null;
  aviso: string;
  entrar: (token: string) => Promise<void>;
  sair: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setTok] = useState<string | null>(null);
  const [sessao, setSessao] = useState<SessaoUsuario | null>(null);
  const [aviso, setAviso] = useState('');
  const tentativa = useRef(0);
  const limiteSessao = useRef(0);

  const limpar = useCallback((mensagem = '') => {
    tentativa.current++;
    setToken(null);
    setTok(null);
    setSessao(null);
    setAviso(mensagem);
  }, []);

  const expirar = useCallback(() => {
    limpar('Sessão expirada ou inválida. Faça login novamente.');
  }, [limpar]);

  useEffect(() => onSessaoInvalida(expirar), [expirar]);

  useEffect(() => {
    if (!sessao) return;
    return agendarExpiracao(limiteSessao.current - performance.now(), expirar);
  }, [sessao, expirar]);

  useEffect(() => () => { tentativa.current++; setToken(null); }, []);

  const entrar = useCallback(async (valor: string) => {
    const atual = ++tentativa.current;
    const candidato = valor.trim().replace(/^Bearer\s+/i, '').trim();
    const inicio = performance.now();
    const identidade = await validarSessao(candidato);
    if (atual !== tentativa.current) throw new Error('Tentativa de login cancelada.');
    // Desconta conservadoramente o tempo da validação e não depende do relógio local.
    limiteSessao.current = inicio + tempoRestanteSessao(identidade);
    if (limiteSessao.current <= performance.now()) throw new Error('Token expirado.');
    setToken(candidato);
    setTok(candidato);
    setSessao(identidade);
    setAviso('');
  }, []);

  const sair = useCallback(() => limpar(), [limpar]);
  const value = useMemo(() => ({ token, sessao, aviso, entrar, sair }), [token, sessao, aviso, entrar, sair]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth fora do AuthProvider');
  return ctx;
}

export function podeAtribuir(papel?: PapelUsuario | null): boolean {
  return papel === 'Admin' || papel === 'Supervisor' || papel === 'Agente';
}
export function ehAdmin(papel?: PapelUsuario | null): boolean {
  return papel === 'Admin';
}
