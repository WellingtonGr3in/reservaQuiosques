import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { adminApi, ApiError } from '../api/client';
import { ErrorBox } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCPF, formatPhone } from '../lib/format';
import { useApi } from '../lib/useApi';
import { useAdminSessionGuard } from './AdminLayout';
import { adminInput, Card, Field, PageHeader, primaryButton, secondaryButton } from './ui';

// RF13 / RN13 / RN14: o administrador reserva para qualquer cliente e qualquer data, já paga ou aguardando o Pix.
export function AdminNewReservationPage() {
  const navigate = useNavigate();
  const kiosks = useApi(() => adminApi.listKiosks(), []);
  useAdminSessionGuard(kiosks.error?.status);

  const [form, setForm] = useState({ kioskId: '', date: '', cpf: '', name: '', email: '', phone: '', paid: true });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const created = await adminApi.createReservation(form);
      navigate(`/admin/reservas/${created.reservation.code}`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
      setBusy(false);
    }
  };

  return (
    <div className="max-w-[720px] mx-auto px-4 sm:px-6 py-8">
      <Link to="/admin/reservas" className="flex items-center gap-1 text-xs text-[#40493d] hover:text-[#0d631b] mb-4">
        <Icon name="arrow_back" className="text-[16px]" /> Reservas
      </Link>
      <PageHeader title="Nova reserva" subtitle="Sem limite de antecedência nem de reservas por CPF. Só não pode haver duas reservas no mesmo quiosque e dia." />

      <Card>
        <form onSubmit={submit} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Field label="Quiosque" htmlFor="n-kiosk">
            <select id="n-kiosk" required value={form.kioskId} onChange={(e) => setForm({ ...form, kioskId: e.target.value })} className={adminInput}>
              <option value="">Selecione...</option>
              {kiosks.data
                ?.filter((k) => k.isActive)
                .map((k) => (
                  <option key={k.id} value={k.id}>
                    {k.name} — até {k.capacity} pessoas
                  </option>
                ))}
            </select>
          </Field>
          <Field label="Data" htmlFor="n-date">
            <input id="n-date" type="date" required value={form.date} onChange={(e) => setForm({ ...form, date: e.target.value })} className={adminInput} />
          </Field>
          <Field label="CPF do cliente" htmlFor="n-cpf">
            <input
              id="n-cpf"
              required
              inputMode="numeric"
              maxLength={14}
              placeholder="000.000.000-00"
              value={form.cpf}
              onChange={(e) => setForm({ ...form, cpf: formatCPF(e.target.value) })}
              className={adminInput}
            />
          </Field>
          <Field label="Nome completo" htmlFor="n-name">
            <input id="n-name" required maxLength={150} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className={adminInput} />
          </Field>
          <Field label="E-mail" htmlFor="n-email">
            <input id="n-email" type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} className={adminInput} />
          </Field>
          <Field label="Telefone" htmlFor="n-phone">
            <input
              id="n-phone"
              type="tel"
              required
              maxLength={15}
              placeholder="(47) 99999-9999"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: formatPhone(e.target.value) })}
              className={adminInput}
            />
          </Field>

          <label className="sm:col-span-2 flex items-start gap-2 p-3 rounded-lg bg-[#f1f3ff] border border-[#dce2f7] cursor-pointer">
            <input type="checkbox" checked={form.paid} onChange={(e) => setForm({ ...form, paid: e.target.checked })} className="mt-0.5 w-4 h-4 accent-[#0d631b]" />
            <span className="text-sm">
              <strong>Pago na administração</strong>
              <span className="block text-xs text-[#40493d]">
                A reserva já nasce confirmada. Desmarque para o cliente pagar via Pix (prazo de 15 minutos).
              </span>
            </span>
          </label>

          {error && (
            <div className="sm:col-span-2">
              <ErrorBox message={error} />
            </div>
          )}

          <div className="sm:col-span-2 flex justify-end gap-2">
            <Link to="/admin/reservas" className={secondaryButton}>
              Cancelar
            </Link>
            <button type="submit" disabled={busy} className={primaryButton}>
              {busy ? 'Salvando...' : 'Criar reserva'}
            </button>
          </div>
        </form>
      </Card>
    </div>
  );
}
