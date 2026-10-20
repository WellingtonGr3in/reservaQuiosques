import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { adminApi, ApiError } from '../api/client';
import { ErrorBox, InfoBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { StatusBadge } from '../components/StatusBadge';
import { formatCurrency, formatDate, formatDateTime, formatTime, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';
import { useAdminSessionGuard } from './AdminLayout';
import { AdminPixCard } from './AdminPixCard';
import { adminInput, Card, dangerButton, Field, PAID_WITH_LABEL, PageHeader, primaryButton, secondaryButton } from './ui';

// RF12, RF14, RF15: detalhe da reserva, alterar quiosque/data e cancelar a qualquer momento (RN13).
export function AdminReservationDetailPage() {
  const { code = '' } = useParams();
  const reservation = useApi(() => adminApi.getReservation(code), [code]);
  const kiosks = useApi(() => adminApi.listKiosks(), []);
  useAdminSessionGuard(reservation.error?.status ?? kiosks.error?.status);

  const [edit, setEdit] = useState({ kioskId: '', date: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [confirmingCancel, setConfirmingCancel] = useState(false);

  useEffect(() => {
    if (reservation.data) setEdit({ kioskId: reservation.data.kioskId, date: reservation.data.reservation.date });
  }, [reservation.data]);

  if (reservation.loading) return <Loading />;
  if (reservation.error || !reservation.data) {
    return (
      <div className="max-w-[860px] mx-auto px-4 py-8">
        <ErrorBox message={reservation.error?.message ?? 'Reserva não encontrada'} />
      </div>
    );
  }

  const { reservation: r, customerEmail, customerPhone, paidWith, isActive } = reservation.data;
  const changed = edit.kioskId !== reservation.data.kioskId || edit.date !== r.date;

  const run = async (action: () => Promise<Parameters<typeof reservation.setData>[0]>, success: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      reservation.setData(await action());
      setNotice(success);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setBusy(false);
      setConfirmingCancel(false);
    }
  };

  return (
    <div className="max-w-[860px] mx-auto px-4 sm:px-6 py-8 flex flex-col gap-5">
      <Link to="/admin/reservas" className="flex items-center gap-1 text-xs text-[#40493d] hover:text-[#0d631b]">
        <Icon name="arrow_back" className="text-[16px]" /> Reservas
      </Link>
      <PageHeader title={`Reserva ${r.code}`} subtitle={`Feita em ${formatDateTime(r.createdAt)}`} actions={<StatusBadge status={r.status} />} />

      {notice && <InfoBox message={notice} />}
      {error && <ErrorBox message={error} />}

      <Card>
        <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-4 text-sm">
          <Item label="Quiosque">
            {r.kioskName} <span className="text-xs text-[#40493d]">(até {r.kioskCapacity} pessoas)</span>
          </Item>
          <Item label="Data">
            {formatDate(r.date)} ({weekday(r.date)}), {formatTime(r.startTime)} às {formatTime(r.endTime)}
          </Item>
          <Item label="Titular">{r.holderName}</Item>
          <Item label="CPF">
            <span className="font-mono">{r.maskedCpf}</span>
          </Item>
          <Item label="E-mail">{customerEmail}</Item>
          <Item label="Telefone">{customerPhone}</Item>
          <Item label="Valor">{formatCurrency(r.amount)}</Item>
          <Item label="Pagamento">{paidWith ? PAID_WITH_LABEL[paidWith] : 'Não pago'}</Item>
        </dl>
      </Card>

      {r.status === 'PENDENTE_PAGAMENTO' && isActive && <AdminPixCard code={r.code} onChanged={reservation.reload} />}

      {isActive ? (
        <>
          <Card>
            <h2 className="text-base font-bold mb-3">Alterar quiosque ou data</h2>
            <form
              onSubmit={(e: FormEvent) => {
                e.preventDefault();
                run(() => adminApi.rescheduleReservation(r.code, edit.kioskId, edit.date), 'Reserva alterada.');
              }}
              className="grid grid-cols-1 sm:grid-cols-3 gap-3 items-end"
            >
              <Field label="Quiosque" htmlFor="e-kiosk">
                <select id="e-kiosk" value={edit.kioskId} onChange={(e) => setEdit({ ...edit, kioskId: e.target.value })} className={adminInput}>
                  {kiosks.data
                    ?.filter((k) => k.isActive || k.id === reservation.data!.kioskId)
                    .map((k) => (
                      <option key={k.id} value={k.id}>
                        {k.name}
                      </option>
                    ))}
                </select>
              </Field>
              <Field label="Data" htmlFor="e-date">
                <input id="e-date" type="date" required value={edit.date} onChange={(e) => setEdit({ ...edit, date: e.target.value })} className={adminInput} />
              </Field>
              <button type="submit" disabled={busy || !changed} className={primaryButton}>
                Salvar alteração
              </button>
            </form>
            <p className="text-xs text-[#707a6c] mt-2">O valor da reserva continua o mesmo.</p>
          </Card>

          <Card className="border-[#ffdad6]">
            <h2 className="text-base font-bold mb-1">Cancelar reserva</h2>
            <p className="text-xs text-[#40493d] mb-3">
              {paidWith === 'PIX'
                ? 'O valor pago via Pix será reembolsado automaticamente.'
                : paidWith === 'ADMINISTRACAO'
                  ? 'Foi paga na administração: o reembolso deve ser feito pela administração.'
                  : 'A reserva ainda não foi paga, então nada será cobrado.'}
            </p>
            {confirmingCancel ? (
              <div className="flex flex-wrap gap-2">
                <button type="button" disabled={busy} onClick={() => run(() => adminApi.cancelReservation(r.code), 'Reserva cancelada.')} className={dangerButton}>
                  {busy ? 'Cancelando...' : 'Confirmar cancelamento'}
                </button>
                <button type="button" disabled={busy} onClick={() => setConfirmingCancel(false)} className={secondaryButton}>
                  Manter reserva
                </button>
              </div>
            ) : (
              <button type="button" onClick={() => setConfirmingCancel(true)} className={dangerButton}>
                <Icon name="cancel" className="text-[18px]" />
                Cancelar reserva
              </button>
            )}
          </Card>
        </>
      ) : (
        <InfoBox message="Esta reserva não está ativa, então não pode ser alterada nem cancelada." />
      )}
    </div>
  );
}

function Item({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col">
      <dt className="text-xs text-[#40493d]">{label}</dt>
      <dd className="font-semibold">{children}</dd>
    </div>
  );
}
