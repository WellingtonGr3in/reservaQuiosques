import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import type { AccessToken, AdminToken } from '../api/types';
import { clearAdminToken, clearToken, getAdminToken, getToken, saveAdminToken, saveToken } from './storage';

interface Auth {
  // Cliente (HU05)
  isLoggedIn: boolean;
  signIn: (token: AccessToken) => void;
  signOut: () => void;
  // Administrador (RF11), com login separado
  isAdmin: boolean;
  signInAdmin: (token: AdminToken) => void;
  signOutAdmin: () => void;
}

const AuthContext = createContext<Auth | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isLoggedIn, setIsLoggedIn] = useState(() => getToken() !== null);
  const [isAdmin, setIsAdmin] = useState(() => getAdminToken() !== null);

  const signIn = useCallback((token: AccessToken) => {
    saveToken(token);
    setIsLoggedIn(true);
  }, []);

  const signOut = useCallback(() => {
    clearToken();
    setIsLoggedIn(false);
  }, []);

  const signInAdmin = useCallback((token: AdminToken) => {
    saveAdminToken(token);
    setIsAdmin(true);
  }, []);

  const signOutAdmin = useCallback(() => {
    clearAdminToken();
    setIsAdmin(false);
  }, []);

  return (
    <AuthContext.Provider value={{ isLoggedIn, signIn, signOut, isAdmin, signInAdmin, signOutAdmin }}>{children}</AuthContext.Provider>
  );
}

export function useAuth(): Auth {
  const auth = useContext(AuthContext);
  if (!auth) throw new Error('useAuth precisa estar dentro de AuthProvider');
  return auth;
}
