import type { ReservationDetails } from '../api/types';
import { formatCurrency, formatDate, formatDateTime, formatTime, weekday } from '../lib/format';
import { Icon } from './Icon';
import { StatusBadge } from './StatusBadge';

// Resultado da tela "Consulta de Reserva" (HU04). Também usado em "Minhas reservas" > "Ver detalhes".
export function ReservationDetailsCard({
  reservation,
  onCancel,
}: {
  reservation: ReservationDetails;
  onCancel: () => void;
}) {
  const paid = reservation.status === 'CONFIRMADA' || reservation.status === 'REALIZADA';

  return (
    <div className="bg-[#ffffff] rounded-xl shadow-md border border-[#dce2f7] overflow-hidden">
      <div className="p-6 md:p-8 flex flex-col md:flex-row md:items-center justify-between gap-4 bg-[#f1f3ff]/50 border-b border-[#dce2f7]">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-[#a3f69c] flex items-center justify-center text-[#005312] shrink-0">
            <Icon name="outdoor_grill" className="text-[28px]" filled />
          </div>
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <h2 className="text-xl font-bold text-[#141b2b]">Reserva {reservation.code}</h2>
              <StatusBadge status={reservation.status} />
            </div>
            <p className="text-xs text-[#40493d] mt-0.5">Feita em {formatDateTime(reservation.createdAt)}</p>
          </div>
        </div>

        <div className="flex md:flex-col items-center md:items-end justify-between border-t md:border-t-0 pt-2 md:pt-0 border-[#dce2f7]">
          <span className="text-xs text-[#40493d]">{paid ? 'Valor pago' : 'Valor'}</span>
          <span className="text-xl font-bold text-[#0d631b]">{formatCurrency(reservation.amount)}</span>
        </div>
      </div>

      <div className="p-6 md:p-8 grid grid-cols-1 md:grid-cols-12 gap-6">
        <div className="md:col-span-5 flex flex-col gap-4">
          <div className="relative w-full h-44 rounded-xl overflow-hidden bg-[#e1e8fd] border border-[#dce2f7]">
            <img className="w-full h-full object-cover" src={reservation.kioskPhotoUrl} alt={reservation.kioskName} />
            <div className="absolute bottom-2 left-2 px-2.5 py-1 rounded bg-[#293040]/80 text-[#edf0ff] text-xs backdrop-blur-sm">
              {reservation.kioskName}
            </div>
          </div>
          <div className="p-4 rounded-xl bg-[#f1f3ff] border border-[#dce2f7] flex items-center justify-between text-[#40493d] text-xs">
            <span>Método de pagamento</span>
            <span className="font-semibold text-[#141b2b] flex items-center gap-1">
              <Icon name="qr_code_2" className="text-[16px] text-[#0d631b]" /> Pix
            </span>
          </div>
        </div>

        <div className="md:col-span-7 grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4 content-start">
          <Field icon="roofing" label="Espaço físico" value={reservation.kioskName} />
          <Field icon="group" label="Capacidade permitida" value={`Até ${reservation.kioskCapacity} pessoas`} />
          <Field icon="event" label="Data agendada" value={`${formatDate(reservation.date)} (${weekday(reservation.date)})`} />
          <Field
            icon="schedule"
            label="Horário reservado"
            value={`${formatTime(reservation.startTime)} às ${formatTime(reservation.endTime)}`}
          />
          <Field icon="person" label="Titular responsável" value={reservation.holderName} />
          <Field icon="lock" label="CPF do titular" value={reservation.maskedCpf} mono />
        </div>
      </div>

      <div className="px-6 md:px-8 py-4 bg-[#f1f3ff] border-t border-[#dce2f7] flex flex-col md:flex-row items-center justify-between gap-4 print:hidden">
        <div className="flex items-start gap-2 max-w-[500px]">
          <Icon name="info" className="text-[#707a6c] text-[20px] shrink-0 mt-0.5" />
          <p className="text-xs text-[#40493d] leading-relaxed">
            {reservation.canCancel ? (
              <>
                O cancelamento {paid ? 'com reembolso integral via Pix ' : ''}está disponível até{' '}
                <strong className="text-[#141b2b] font-semibold">{formatDateTime(reservation.cancellationDeadline)}</strong> (48h de
                antecedência).
              </>
            ) : (
              'Esta reserva não pode mais ser cancelada.'
            )}
          </p>
        </div>

        <div className="flex items-center gap-3 w-full md:w-auto shrink-0 justify-end">
          {reservation.canCancel && (
            <button
              type="button"
              onClick={onCancel}
              className="px-4 h-10 rounded-lg text-[#ba1a1a] hover:bg-[#ffdad6] hover:text-[#93000a] text-sm font-semibold transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              <Icon name="cancel" className="text-[18px]" />
              <span>Cancelar reserva</span>
            </button>
          )}
          {paid && (
            <button
              type="button"
              onClick={() => window.print()}
              className="px-4 h-10 rounded-lg bg-[#ffffff] hover:bg-[#e9edff] border border-[#dce2f7] shadow-sm text-[#141b2b] text-sm font-semibold transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              <Icon name="print" className="text-[18px]" />
              <span>Imprimir comprovante</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function Field({ icon, label, value, mono = false }: { icon: string; label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex flex-col">
      <span className="text-xs text-[#40493d] mb-0.5">{label}</span>
      <span className={`text-sm text-[#141b2b] font-semibold flex items-center gap-1.5 ${mono ? 'font-mono' : ''}`}>
        <Icon name={icon} className="text-[#0d631b] text-[18px]" />
        {value}
      </span>
    </div>
  );
}
