import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { adminApi } from '../api/client';
import type { ReservationFilter } from '../api/types';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { StatusBadge } from '../components/StatusBadge';
import { formatCPF, formatCurrency, formatDate, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';
import { useAdminSessionGuard } from './AdminLayout';
import { adminInput, Card, Field, PAID_WITH_LABEL, PageHeader, primaryButton, secondaryButton } from './ui';

const EMPTY: ReservationFilter = { from: '', to: '', kioskId: '', status: '', cpf: '' };

// RF12: lista de todas as reservas, com filtros por data, quiosque, status e CPF.
export function AdminReservationsPage() {
  const [form, setForm] = useState<ReservationFilter>(EMPTY);
  const [filter, setFilter] = useState<ReservationFilter>(EMPTY);
  const kiosks = useApi(() => adminApi.listKiosks(), []);
  const reservations = useApi(() => adminApi.searchReservations(clean(filter)), [filter]);
  useAdminSessionGuard(reservations.error?.status ?? kiosks.error?.status);

  const apply = (e: FormEvent) => {
    e.preventDefault();
    setFilter({ ...form });
  };

  return (
    <div className="max-w-[1100px] mx-auto px-4 sm:px-6 py-8">
      <PageHeader
        title="Reservas"
        subtitle="Todas as reservas, de qualquer cliente."
        actions={
          <Link to="/admin/reservas/nova" className={primaryButton}>
            <Icon name="add" className="text-[20px]" />
            Nova reserva
          </Link>
        }
      />

      <Card className="mb-6">
        <form onSubmit={apply} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-3 items-end">
          <Field label="De" htmlFor="f-from">
            <input id="f-from" type="date" value={form.from} onChange={(e) => setForm({ ...form, from: e.target.value })} className={adminInput} />
          </Field>
          <Field label="Até" htmlFor="f-to">
            <input id="f-to" type="date" value={form.to} onChange={(e) => setForm({ ...form, to: e.target.value })} className={adminInput} />
          </Field>
          <Field label="Quiosque" htmlFor="f-kiosk">
            <select id="f-kiosk" value={form.kioskId} onChange={(e) => setForm({ ...form, kioskId: e.target.value })} className={adminInput}>
              <option value="">Todos</option>
              {kiosks.data?.map((k) => (
                <option key={k.id} value={k.id}>
                  {k.name}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Status" htmlFor="f-status">
            <select id="f-status" value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })} className={adminInput}>
              <option value="">Todos</option>
              <option value="PENDENTE_PAGAMENTO">Aguardando pagamento</option>
              <option value="CONFIRMADA">Confirmada</option>
              <option value="CANCELADA">Cancelada</option>
              <option value="EXPIRADA">Expirada</option>
            </select>
          </Field>
          <Field label="CPF" htmlFor="f-cpf">
            <input
              id="f-cpf"
              inputMode="numeric"
              maxLength={14}
              placeholder="000.000.000-00"
              value={form.cpf}
              onChange={(e) => setForm({ ...form, cpf: formatCPF(e.target.value) })}
              className={adminInput}
            />
          </Field>
          <div className="flex gap-2">
            <button type="submit" className={`${primaryButton} flex-1`}>
              <Icon name="search" className="text-[18px]" />
              Filtrar
            </button>
            <button
              type="button"
              title="Limpar filtros"
              onClick={() => {
                setForm(EMPTY);
                setFilter(EMPTY);
              }}
              className={secondaryButton}
            >
              <Icon name="filter_alt_off" className="text-[18px]" />
            </button>
          </div>
        </form>
      </Card>

      {reservations.loading && <Loading />}
      {reservations.error && <ErrorBox message={reservations.error.message} />}
      {reservations.data && (
        <section className="bg-[#ffffff] rounded-xl shadow-sm border border-[#dce2f7] overflow-hidden">
          {reservations.data.length === 0 ? (
            <p className="p-8 text-center text-sm text-[#40493d]">Nenhuma reserva encontrada.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-sm">
                <thead>
                  <tr className="bg-[#f1f3ff] text-[#40493d] text-xs font-semibold border-b border-[#dce2f7]">
                    <th className="py-3 px-4">Código</th>
                    <th className="py-3 px-4">Quiosque</th>
                    <th className="py-3 px-4">Data</th>
                    <th className="py-3 px-4">Titular</th>
                    <th className="py-3 px-4">Status</th>
                    <th className="py-3 px-4">Pagamento</th>
                    <th className="py-3 px-4 text-right">Valor</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#dce2f7]/60">
                  {reservations.data.map(({ reservation: r, paidWith }) => (
                    <tr key={r.code} className="hover:bg-[#f1f3ff]/50">
                      <td className="py-3 px-4 whitespace-nowrap">
                        <Link to={`/admin/reservas/${r.code}`} className="font-mono text-xs font-semibold text-[#0d631b] hover:underline">
                          {r.code}
                        </Link>
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap">{r.kioskName}</td>
                      <td className="py-3 px-4 whitespace-nowrap">
                        {formatDate(r.date)} <span className="text-xs text-[#40493d]">({weekday(r.date).slice(0, 3)})</span>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex flex-col">
                          <span>{r.holderName}</span>
                          <span className="font-mono text-xs text-[#40493d]">{r.maskedCpf}</span>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <StatusBadge status={r.status} />
                      </td>
                      <td className="py-3 px-4 whitespace-nowrap text-xs text-[#40493d]">{paidWith ? PAID_WITH_LABEL[paidWith] : '—'}</td>
                      <td className="py-3 px-4 text-right whitespace-nowrap font-semibold">{formatCurrency(r.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </div>
  );
}

// Campos vazios não filtram.
function clean(filter: ReservationFilter): ReservationFilter {
  return Object.fromEntries(Object.entries(filter).filter(([, v]) => v)) as ReservationFilter;
}
