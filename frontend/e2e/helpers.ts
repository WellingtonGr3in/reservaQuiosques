import { expect, type APIRequestContext } from '@playwright/test';

// Dados de apoio dos testes E2E. Cada teste usa um CPF novo e uma data livre,
// para não esbarrar no limite de 2 reservas por CPF (RN08) nem em datas já ocupadas (RN01).

export type Kiosk = { id: string; name: string; dailyRate: number };

// CPF aleatório com dígitos verificadores válidos (RN04).
export function randomCpf(): string {
  const digits = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  if (digits.every((d) => d === digits[0])) digits[0] = (digits[0] + 1) % 10;
  for (const length of [9, 10]) {
    const sum = digits.slice(0, length).reduce((acc, d, i) => acc + d * (length + 1 - i), 0);
    const rest = (sum * 10) % 11;
    digits.push(rest === 10 ? 0 : rest);
  }
  return digits.join('');
}

export const formatCpf = (cpf: string) => cpf.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, '$1.$2.$3-$4');

// RNF07: o CPF aparece como ***.982.247-**.
export const maskedCpf = (cpf: string) => `***.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-**`;

export async function listKiosks(request: APIRequestContext): Promise<Kiosk[]> {
  const response = await request.get('/api/quiosques');
  expect(response.ok()).toBeTruthy();
  return response.json();
}

// Procura, pela API, um dia "LIVRE" no calendário de algum quiosque, a partir de 10 dias à frente.
// Começa num ponto aleatório para dois testes seguidos não disputarem a mesma data.
export async function findFreeDay(request: APIRequestContext): Promise<{ kiosk: Kiosk; date: string }> {
  const kiosks = await listKiosks(request);
  const start = new Date();
  start.setDate(start.getDate() + 10 + Math.floor(Math.random() * 60));

  for (let offset = 0; offset < kiosks.length * 2; offset++) {
    const kiosk = kiosks[(Math.floor(Math.random() * kiosks.length) + offset) % kiosks.length];
    for (const month of [0, 1]) {
      const d = new Date(start.getFullYear(), start.getMonth() + month, 1);
      const response = await request.get(`/api/quiosques/${kiosk.id}/calendario?ano=${d.getFullYear()}&mes=${d.getMonth() + 1}`);
      const calendar: { days: { date: string; status: string }[] } = await response.json();
      const startIso = start.toISOString().slice(0, 10);
      const free = calendar.days.find((day) => day.status === 'LIVRE' && day.date >= startIso);
      if (free) return { kiosk, date: free.date };
    }
  }
  throw new Error('Nenhum dia livre encontrado para o teste');
}

// Cria uma reserva direto pela API (preparação de cenário).
export async function createReservation(
  request: APIRequestContext,
  data: { kioskId: string; date: string; cpf: string; name?: string },
): Promise<{ code: string; status: string }> {
  const response = await request.post('/api/reservas', {
    data: { name: 'Cliente E2E', email: 'e2e@exemplo.com', phone: '47999990000', ...data },
  });
  expect(response.status()).toBe(201);
  return response.json();
}

export async function getReceipt(request: APIRequestContext, code: string, cpf: string) {
  const response = await request.post(`/api/reservas/${code}/comprovante`, { data: { cpf } });
  expect(response.ok()).toBeTruthy();
  return response.json() as Promise<{ code: string; status: string; maskedCpf: string }>;
}

export async function getCalendarDay(request: APIRequestContext, kioskId: string, date: string): Promise<string> {
  const [year, month] = date.split('-').map(Number);
  const response = await request.get(`/api/quiosques/${kioskId}/calendario?ano=${year}&mes=${month}`);
  const calendar: { days: { date: string; status: string }[] } = await response.json();
  return calendar.days.find((d) => d.date === date)!.status;
}
