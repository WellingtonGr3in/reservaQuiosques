import type { ReservationStatus } from '../api/types';

const STYLES: Record<ReservationStatus, { label: string; badge: string; dot: string }> = {
  PENDENTE_PAGAMENTO: { label: 'Aguardando pagamento', badge: 'bg-[#fff4d6] text-[#7a5400]', dot: 'bg-[#b07a00]' },
  CONFIRMADA: { label: 'Confirmada', badge: 'bg-[#acf4a4] text-[#0c5216]', dot: 'bg-[#0d631b]' },
  REALIZADA: { label: 'Realizada', badge: 'bg-[#e9edff] text-[#40493d]', dot: 'bg-[#707a6c]' },
  CANCELADA: { label: 'Cancelada', badge: 'bg-[#ffdad6] text-[#93000a]', dot: 'bg-[#ba1a1a]' },
  EXPIRADA: { label: 'Expirada', badge: 'bg-[#e9edff] text-[#40493d]', dot: 'bg-[#707a6c]' },
};

export function StatusBadge({ status }: { status: ReservationStatus }) {
  const style = STYLES[status];
  return (
    <span className={`inline-flex items-center gap-1.5 rounded px-2.5 py-1 text-xs font-semibold whitespace-nowrap ${style.badge}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${style.dot}`}></span>
      {style.label}
    </span>
  );
}
