import type { AccessToken } from '../api/types';

// Tokens JWT (RNF04: expiram em até 60 minutos). Cliente e administrador têm logins separados (RF11).
const CUSTOMER_KEY = 'quiosques.auth';
const ADMIN_KEY = 'quiosques.admin';

function read(key: string): AccessToken | null {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return null;
    const value = JSON.parse(raw) as AccessToken;
    return new Date(value.expiresAt).getTime() > Date.now() ? value : null;
  } catch {
    return null;
  }
}

function write(key: string, token: AccessToken | null) {
  try {
    if (token) localStorage.setItem(key, JSON.stringify(token));
    else localStorage.removeItem(key);
  } catch {
    // Sem armazenamento disponível: o login vale só até recarregar a página.
  }
}

export const getToken = () => read(CUSTOMER_KEY)?.token ?? null;
export const saveToken = (token: AccessToken) => write(CUSTOMER_KEY, token);
export const clearToken = () => write(CUSTOMER_KEY, null);

export const getAdminToken = () => read(ADMIN_KEY)?.token ?? null;
export const saveAdminToken = (token: AccessToken) => write(ADMIN_KEY, token);
export const clearAdminToken = () => write(ADMIN_KEY, null);
