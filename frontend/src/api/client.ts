import { getAdminToken, getToken } from '../auth/storage';
import type {
  AdminKiosk,
  AdminKioskInput,
  AdminNewReservation,
  AdminReservation,
  AdminToken,
  ReservationFilter,
  AccessToken,
  CreatedReservation,
  CustomerProfile,
  KioskCalendar,
  KioskDetails,
  KioskSummary,
  MyReservations,
  PixPayment,
  Receipt,
  ReservationDetails,
} from './types';

// Erro da API. A mensagem já vem em português, pronta para exibir ({ "message": "..." }).
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

const FALLBACK_MESSAGES: Record<number, string> = {
  401: 'Sua sessão expirou. Entre novamente.',
  429: 'Muitas tentativas. Aguarde um minuto e tente novamente.',
  // nginx sem resposta da API (ex.: o container ainda está subindo).
  502: 'O servidor está iniciando. Tente novamente em alguns segundos.',
  503: 'O servidor está iniciando. Tente novamente em alguns segundos.',
};

// "auth": qual token enviar (cliente ou administrador).
async function request<T>(method: string, path: string, body?: unknown, auth?: 'cliente' | 'admin'): Promise<T> {
  const headers: Record<string, string> = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (auth) {
    const token = auth === 'admin' ? getAdminToken() : getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  } catch {
    throw new ApiError(0, 'Não foi possível conectar ao servidor. Tente novamente.');
  }

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    const message = data?.message ?? FALLBACK_MESSAGES[response.status] ?? 'Ocorreu um erro inesperado. Tente novamente.';
    throw new ApiError(response.status, message);
  }

  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  // HU01
  listKiosks: () => request<KioskSummary[]>('GET', '/api/quiosques'),
  getKiosk: (id: string) => request<KioskDetails>('GET', `/api/quiosques/${id}`),
  getCalendar: (id: string, year: number, month: number) =>
    request<KioskCalendar>('GET', `/api/quiosques/${id}/calendario?ano=${year}&mes=${month}`),

  // HU02
  createReservation: (data: { kioskId: string; date: string; cpf: string; name: string; email: string; phone: string }) =>
    request<CreatedReservation>('POST', '/api/reservas', data),

  // HU03 — o CPF vai sempre no corpo, nunca na URL.
  startPayment: (code: string, cpf: string) => request<PixPayment>('POST', `/api/reservas/${code}/pagamento`, { cpf }),
  getReceipt: (code: string, cpf: string) => request<Receipt>('POST', `/api/reservas/${code}/comprovante`, { cpf }),
  simulatePayment: (transactionId: string) => request<unknown>('POST', `/api/dev/pagamentos/${transactionId}/aprovar`),

  // HU04
  // HU04: consulta sem login só com o CPF (RN10); devolve todas as reservas do CPF.
  lookup: (cpf: string) => request<MyReservations>('POST', '/api/reservas/consulta', { cpf }),
  cancel: (code: string, cpf: string) => request<ReservationDetails>('POST', `/api/reservas/${code}/cancelamento`, { cpf }),

  // HU05
  createAccount: (data: { cpf: string; email: string; password: string }) =>
    request<AccessToken>('POST', '/api/contas', data),
  login: (email: string, password: string) => request<AccessToken>('POST', '/api/login', { email, password }),
  myReservations: () => request<MyReservations>('GET', '/api/minhas-reservas', undefined, 'cliente'),
  myReservation: (code: string) => request<ReservationDetails>('GET', `/api/minhas-reservas/${code}`, undefined, 'cliente'),
  cancelMine: (code: string) => request<ReservationDetails>('POST', `/api/minhas-reservas/${code}/cancelamento`, undefined, 'cliente'),
  // Reserva com login: dados pré-preenchidos e CPF da conta (o CPF completo não volta para o navegador).
  myProfile: () => request<CustomerProfile>('GET', '/api/minha-conta', undefined, 'cliente'),
  createMyReservation: (data: { kioskId: string; date: string; name: string; email: string; phone: string }) =>
    request<CreatedReservation>('POST', '/api/minhas-reservas', data, 'cliente'),
  startMyPayment: (code: string) => request<PixPayment>('POST', `/api/minhas-reservas/${code}/pagamento`, {}, 'cliente'),
};

// Área de administração (HU06, HU07).
export const adminApi = {
  login: (email: string, password: string) => request<AdminToken>('POST', '/api/admin/login', { email, password }),

  searchReservations: (filter: ReservationFilter) => request<AdminReservation[]>('POST', '/api/admin/reservas/busca', filter, 'admin'),
  getReservation: (code: string) => request<AdminReservation>('GET', `/api/admin/reservas/${code}`, undefined, 'admin'),
  createReservation: (data: AdminNewReservation) => request<AdminReservation>('POST', '/api/admin/reservas', data, 'admin'),
  rescheduleReservation: (code: string, kioskId: string, date: string) =>
    request<AdminReservation>('PUT', `/api/admin/reservas/${code}`, { kioskId, date }, 'admin'),
  cancelReservation: (code: string) => request<AdminReservation>('POST', `/api/admin/reservas/${code}/cancelamento`, {}, 'admin'),
  startPayment: (code: string) => request<PixPayment>('POST', `/api/admin/reservas/${code}/pagamento`, {}, 'admin'),

  listKiosks: () => request<AdminKiosk[]>('GET', '/api/admin/quiosques', undefined, 'admin'),
  getKiosk: (id: string) => request<AdminKiosk>('GET', `/api/admin/quiosques/${id}`, undefined, 'admin'),
  createKiosk: (data: AdminKioskInput) => request<AdminKiosk>('POST', '/api/admin/quiosques', data, 'admin'),
  updateKiosk: (id: string, data: AdminKioskInput) => request<AdminKiosk>('PUT', `/api/admin/quiosques/${id}`, data, 'admin'),
  deleteKiosk: (id: string) => request<void>('DELETE', `/api/admin/quiosques/${id}`, undefined, 'admin'),
  // RF16: envia uma foto do computador e devolve o endereço dela.
  uploadImage: async (file: File): Promise<string> => {
    const form = new FormData();
    form.append('arquivo', file);
    const headers: Record<string, string> = {};
    const token = getAdminToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    let response: Response;
    try {
      response = await fetch('/api/admin/imagens', { method: 'POST', headers, body: form });
    } catch {
      throw new ApiError(0, 'Não foi possível conectar ao servidor. Tente novamente.');
    }
    const data = await response.json().catch(() => null);
    if (!response.ok) {
      throw new ApiError(response.status, data?.message ?? FALLBACK_MESSAGES[response.status] ?? 'Não foi possível enviar a imagem.');
    }
    return (data as { url: string }).url;
  },
};
