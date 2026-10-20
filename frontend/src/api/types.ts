// Contratos da API (backend/src/Quiosques.Api/Endpoints).

export interface KioskSummary {
  id: string;
  name: string;
  photoUrl: string;
  capacity: number;
  dailyRate: number;
  items: string[];
}

export interface KioskDetails {
  id: string;
  name: string;
  description: string;
  capacity: number;
  dailyRate: number;
  photoUrl: string;
  usageRules: string;
  items: string[];
  photos: string[];
}

export type DayStatus = 'LIVRE' | 'OCUPADO' | 'INDISPONIVEL';

export interface KioskCalendar {
  kioskId: string;
  year: number;
  month: number;
  days: { date: string; status: DayStatus }[];
}

// "REALIZADA" só aparece em "Minhas reservas" (confirmada com data passada).
export type ReservationStatus = 'PENDENTE_PAGAMENTO' | 'CONFIRMADA' | 'EXPIRADA' | 'CANCELADA' | 'REALIZADA';

export interface CreatedReservation {
  code: string;
  kioskId: string;
  date: string;
  amount: number;
  status: ReservationStatus;
  expiresAt: string;
}

export interface PixPayment {
  reservationCode: string;
  amount: number;
  pixCopyPaste: string;
  expiresAt: string;
  transactionId: string;
}

export interface Receipt {
  code: string;
  kioskName: string;
  kioskCapacity: number;
  date: string;
  startTime: string;
  endTime: string;
  holderName: string;
  maskedCpf: string;
  amount: number;
  status: ReservationStatus;
}

export interface ReservationDetails extends Receipt {
  kioskPhotoUrl: string;
  createdAt: string;
  cancellationDeadline: string;
  canCancel: boolean;
}

export interface MyReservations {
  holderName: string;
  maskedCpf: string;
  reservations: ReservationDetails[];
}

export interface AccessToken {
  token: string;
  expiresAt: string;
}

// ---------- Área de administração (HU06, HU07) ----------

export interface AdminToken extends AccessToken {
  name: string;
}

export interface ReservationFilter {
  from?: string;
  to?: string;
  kioskId?: string;
  status?: string;
  cpf?: string;
}

export interface AdminReservation {
  reservation: ReservationDetails;
  kioskId: string;
  customerEmail: string;
  customerPhone: string;
  paidWith: 'PIX' | 'ADMINISTRACAO' | null;
  isActive: boolean;
}

export interface AdminNewReservation {
  kioskId: string;
  date: string;
  cpf: string;
  name: string;
  email: string;
  phone: string;
  paid: boolean;
}

export interface AdminKioskInput {
  name: string;
  description: string;
  capacity: number;
  dailyRate: number;
  photoUrl: string;
  usageRules: string;
  isActive: boolean;
  items: string[];
  photos: string[];
}

export interface AdminKiosk extends AdminKioskInput {
  id: string;
  hasReservations: boolean;
}

// Cliente logado (pré-preenche a reserva). CPF mascarado (RNF07).
export interface CustomerProfile {
  name: string;
  email: string;
  phone: string;
  maskedCpf: string;
}
