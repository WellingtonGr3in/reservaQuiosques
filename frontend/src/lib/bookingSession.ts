// Guarda como acessar a reserva que acabou de ser feita, para as telas de pagamento e comprovante.
// Sem login, a API exige código + CPF (RN10): o CPF fica só nesta aba do navegador e nunca vai para a URL.
// Com login, a reserva é acessada pela conta e o CPF completo nem chega ao navegador.
const KEY = 'quiosques.reservas';

export const ACCOUNT_BOOKING = 'conta';

function readAll(): Record<string, string> {
  try {
    return JSON.parse(sessionStorage.getItem(KEY) ?? '{}');
  } catch {
    return {};
  }
}

export function rememberBooking(code: string, cpfOrAccount: string) {
  try {
    sessionStorage.setItem(KEY, JSON.stringify({ ...readAll(), [code]: cpfOrAccount }));
  } catch {
    // Sem armazenamento: o fluxo funciona até recarregar a página.
  }
}

export function bookingCpf(code: string): string | null {
  return readAll()[code] ?? null;
}
