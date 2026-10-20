import { useMemo } from 'react';
import { api } from '../api/client';
import type { PixPayment, Receipt } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { ACCOUNT_BOOKING, bookingCpf } from './bookingSession';

export interface BookingAccess {
  startPayment: () => Promise<PixPayment>;
  getReceipt: () => Promise<Receipt>;
}

// Como as telas de pagamento e comprovante acessam a reserva:
// - feita sem conta neste navegador: código + CPF guardado na aba (RN10);
// - feita com a conta (ou aberta em "Minhas reservas"): pelo login.
// Devolve null se não houver como acessar (aí a tela manda para "Consultar reserva").
// O objeto é estável entre renderizações, para poder ser dependência de useEffect.
export function useBookingAccess(code: string): BookingAccess | null {
  const { isLoggedIn } = useAuth();
  const stored = bookingCpf(code);

  return useMemo(() => {
    if (stored && stored !== ACCOUNT_BOOKING) {
      return {
        startPayment: () => api.startPayment(code, stored),
        getReceipt: () => api.getReceipt(code, stored),
      };
    }

    if (isLoggedIn) {
      return {
        startPayment: () => api.startMyPayment(code),
        getReceipt: () => api.myReservation(code),
      };
    }

    return null;
  }, [code, stored, isLoggedIn]);
}
