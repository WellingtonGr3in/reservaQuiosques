import type { ReservationDetails } from '../api/types';
import { formatCurrency, formatDate } from '../lib/format';
import { ErrorBox } from './Feedback';
import { Icon } from './Icon';

// RN07: confirmada é cancelada com reembolso integral; aguardando pagamento, sem cobrança.
export function CancelModal({
  reservation,
  busy,
  error,
  onConfirm,
  onClose,
}: {
  reservation: ReservationDetails;
  busy: boolean;
  error: string | null;
  onConfirm: () => void;
  onClose: () => void;
}) {
  const paid = reservation.status === 'CONFIRMADA';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#293040]/40 backdrop-blur-sm p-4" role="dialog" aria-modal="true">
      <div className="w-full max-w-md bg-[#ffffff] rounded-xl shadow-xl border border-[#dce2f7] p-6 flex flex-col gap-4">
        <div className="flex items-center justify-between text-[#ba1a1a]">
          <div className="flex items-center gap-2">
            <Icon name="warning" className="text-[28px]" />
            <h3 className="text-lg font-bold text-[#141b2b]">Confirmar cancelamento</h3>
          </div>
          <button type="button" onClick={onClose} disabled={busy} className="text-[#40493d] hover:text-[#141b2b] p-1" aria-label="Fechar">
            <Icon name="close" className="text-[20px]" />
          </button>
        </div>

        <p className="text-sm text-[#40493d] leading-relaxed">
          Tem certeza de que deseja cancelar a reserva <strong className="text-[#141b2b] font-mono">{reservation.code}</strong> (
          <span className="font-semibold text-[#141b2b]">{reservation.kioskName}</span>) para o dia{' '}
          <strong className="text-[#141b2b]">{formatDate(reservation.date)}</strong>?
        </p>

        <div className="p-3.5 rounded-lg bg-[#f1f3ff] border border-[#dce2f7] text-xs text-[#141b2b] flex flex-col gap-1">
          {paid ? (
            <>
              <span className="font-bold text-[#0d631b]">Reembolso integral de {formatCurrency(reservation.amount)}</span>
              <span className="text-[#40493d]">O valor será devolvido via Pix.</span>
            </>
          ) : (
            <span className="text-[#40493d]">A reserva ainda não foi paga, então nada será cobrado.</span>
          )}
        </div>

        {error && <ErrorBox message={error} />}

        <div className="flex items-center justify-end gap-3 mt-2">
          <button
            type="button"
            onClick={onClose}
            disabled={busy}
            className="px-4 h-10 rounded-lg bg-[#f1f3ff] hover:bg-[#e9edff] text-[#141b2b] text-xs font-semibold transition-colors border border-[#dce2f7] cursor-pointer"
          >
            Manter reserva
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={busy}
            className="px-4 h-10 rounded-lg bg-[#ba1a1a] hover:bg-[#93000a] text-white text-xs font-bold transition-colors shadow-sm cursor-pointer disabled:opacity-70"
          >
            {busy ? 'Cancelando...' : 'Sim, cancelar reserva'}
          </button>
        </div>
      </div>
    </div>
  );
}
