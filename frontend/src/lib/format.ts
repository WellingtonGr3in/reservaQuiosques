// Formatação para exibição. As regras (CPF válido, prazos etc.) são conferidas pela API.

export function formatCPF(value: string): string {
  const d = value.replace(/\D/g, '').slice(0, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

export function formatPhone(value: string): string {
  const d = value.replace(/\D/g, '').slice(0, 11);
  if (d.length === 0) return '';
  if (d.length <= 2) return `(${d}`;
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

export function formatCurrency(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

// "2026-11-20" -> Date local (sem deslocamento de fuso).
export function parseDate(iso: string): Date {
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function toIsoDate(date: Date): string {
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${m}-${d}`;
}

// "2026-11-20" -> "20/11/2026"
export function formatDate(iso: string): string {
  return parseDate(iso).toLocaleDateString('pt-BR');
}

// "2026-11-20" -> "Sexta-feira"
export function weekday(iso: string): string {
  const name = parseDate(iso).toLocaleDateString('pt-BR', { weekday: 'long' });
  return name.charAt(0).toUpperCase() + name.slice(1);
}

// "08:00:00" -> "08:00"
export function formatTime(time: string): string {
  return time.slice(0, 5);
}

const SAO_PAULO = 'America/Sao_Paulo';

// Instante (ISO com fuso) -> "02/11/2026 às 14:32", no horário de Brasília.
export function formatDateTime(instant: string): string {
  const date = new Date(instant);
  const day = date.toLocaleDateString('pt-BR', { timeZone: SAO_PAULO });
  const time = date.toLocaleTimeString('pt-BR', { timeZone: SAO_PAULO, hour: '2-digit', minute: '2-digit' });
  return `${day} às ${time}`;
}

export function monthName(year: number, month: number): string {
  const name = new Date(year, month - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });
  return name.charAt(0).toUpperCase() + name.slice(1).replace(' de ', ' ');
}
