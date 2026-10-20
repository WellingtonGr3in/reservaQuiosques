import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import { api, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { Stepper } from '../components/Stepper';
import { ACCOUNT_BOOKING, rememberBooking } from '../lib/bookingSession';
import { formatCPF, formatCurrency, formatDate, formatPhone, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';

const inputClass =
  'w-full h-10 px-3 bg-[#ffffff] text-[#141b2b] text-sm border border-[#d1d5db] rounded focus:border-[#2e7d32] focus:ring-1 focus:ring-[#2e7d32] outline-none transition-colors';

// Tela "Seus Dados" (HU02 / RF04): reserva sem conta, com CPF, nome, e-mail e telefone.
// Logado (HU05): nome, e-mail e telefone já vêm preenchidos e a reserva usa o CPF da conta.
export function CustomerDataPage() {
  const { id = '' } = useParams();
  const [searchParams] = useSearchParams();
  const date = searchParams.get('data');
  const navigate = useNavigate();
  const { isLoggedIn, signOut } = useAuth();
  const kiosk = useApi(() => api.getKiosk(id), [id]);

  const [form, setForm] = useState({ cpf: '', name: '', email: '', phone: '', accepted: false });
  const [maskedCpf, setMaskedCpf] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    if (!isLoggedIn) return;
    api
      .myProfile()
      .then((profile) => {
        setMaskedCpf(profile.maskedCpf);
        setForm((f) => ({ ...f, name: profile.name, email: profile.email, phone: formatPhone(profile.phone) }));
      })
      .catch((err) => {
        // Sessão vencida: segue como reserva sem conta.
        if (err instanceof ApiError && err.status === 401) signOut();
      });
  }, [isLoggedIn, signOut]);

  const logged = isLoggedIn && maskedCpf !== null;

  if (!date) {
    return (
      <div className="max-w-[1100px] mx-auto px-6 py-8">
        <ErrorBox message="Escolha um dia antes de continuar.">
          <Link to={`/quiosques/${id}`} className="underline">
            Voltar ao calendário
          </Link>
        </ErrorBox>
      </div>
    );
  }
  if (kiosk.loading) return <Loading />;
  if (kiosk.error || !kiosk.data) {
    return (
      <div className="max-w-[1100px] mx-auto px-6 py-8">
        <ErrorBox message={kiosk.error?.message ?? 'Quiosque não encontrado'} />
      </div>
    );
  }
  const k = kiosk.data;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const contact = { kioskId: k.id, date, name: form.name, email: form.email, phone: form.phone };
      const reservation = logged
        ? await api.createMyReservation(contact)
        : await api.createReservation({ ...contact, cpf: form.cpf });
      rememberBooking(reservation.code, logged ? ACCOUNT_BOOKING : form.cpf);
      navigate(`/reservas/${reservation.code}/pagamento`);
    } catch (err) {
      setError(err instanceof ApiError ? err : new ApiError(0, String(err)));
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8">
      <Stepper steps={['Quiosque e Data', 'Seus Dados', 'Pagamento']} current={1} />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        <section className="lg:col-span-8 bg-[#ffffff] p-6 rounded-xl border border-[#dce2f7] shadow-sm">
          <div className="mb-6">
            <h1 className="text-2xl font-bold text-[#141b2b] tracking-tight">Identificação do responsável</h1>
            <p className="text-sm text-[#40493d] mt-1">
              {logged
                ? 'Confira seus dados. Eles vieram da sua conta e podem ser alterados.'
                : 'Preencha seus dados para prosseguir com a reserva do quiosque. Não é preciso criar conta.'}
            </p>
          </div>

          <form onSubmit={submit} className="flex flex-col gap-4">
            <Field
              label="CPF"
              htmlFor="cpf"
              hint={logged ? 'CPF da sua conta. A reserva fica no seu nome.' : 'Documento que será exigido na portaria de acesso.'}
            >
              {logged ? (
                <input id="cpf" readOnly value={maskedCpf ?? ''} className={`${inputClass} bg-[#f1f3ff] font-mono text-[#40493d] cursor-not-allowed`} />
              ) : (
                <input
                  id="cpf"
                  required
                  inputMode="numeric"
                  maxLength={14}
                  value={form.cpf}
                  onChange={(e) => setForm({ ...form, cpf: formatCPF(e.target.value) })}
                  placeholder="000.000.000-00"
                  className={inputClass}
                />
              )}
            </Field>
            <Field label="Nome completo" htmlFor="name">
              <input
                id="name"
                required
                maxLength={150}
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="Digite seu nome completo"
                className={inputClass}
              />
            </Field>
            <Field label="E-mail" htmlFor="email" hint="O comprovante da reserva será enviado para este endereço.">
              <input
                id="email"
                type="email"
                required
                maxLength={254}
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                placeholder="seuemail@exemplo.com.br"
                className={inputClass}
              />
            </Field>
            <Field label="Telefone / WhatsApp" htmlFor="phone">
              <input
                id="phone"
                type="tel"
                required
                maxLength={15}
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: formatPhone(e.target.value) })}
                placeholder="(47) 99999-9999"
                className={inputClass}
              />
            </Field>

            {/* O aceite é só uma confirmação na tela; não é gravado (docs/03-prototipos). */}
            <div className="flex items-start gap-2 pt-2">
              <input
                id="accepted"
                type="checkbox"
                required
                checked={form.accepted}
                onChange={(e) => setForm({ ...form, accepted: e.target.checked })}
                className="mt-1 w-4 h-4 accent-[#0d631b] cursor-pointer"
              />
              <label htmlFor="accepted" className="text-xs text-[#40493d] cursor-pointer leading-normal">
                Declaro que li e concordo com as regras de uso das churrasqueiras coletivas e me responsabilizo pelo espaço.
              </label>
            </div>

            {error && (
              <ErrorBox message={error.message}>
                {error.status === 409 && (
                  <Link to={`/quiosques/${k.id}`} className="underline">
                    Escolher outro dia
                  </Link>
                )}
              </ErrorBox>
            )}

            <div className="pt-4 flex items-center justify-between flex-wrap gap-4 border-t border-[#dce2f7]">
              <Link
                to={`/quiosques/${k.id}?data=${date}`}
                className="inline-flex items-center gap-1 text-sm text-[#40493d] hover:text-[#141b2b] transition-colors py-2"
              >
                <Icon name="arrow_back" className="text-[18px]" />
                <span>Voltar</span>
              </Link>
              <button
                type="submit"
                disabled={submitting}
                className="bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-semibold h-10 px-6 rounded-lg inline-flex items-center gap-2 shadow-sm transition-colors cursor-pointer disabled:opacity-70"
              >
                <span>{submitting ? 'Reservando...' : 'Ir para o pagamento'}</span>
                <Icon name="arrow_forward" className="text-[18px]" />
              </button>
            </div>
          </form>
        </section>

        <aside className="lg:col-span-4 bg-[#f1f3ff] p-6 rounded-xl border border-[#dce2f7] shadow-sm">
          <h2 className="text-lg font-bold text-[#141b2b] tracking-tight pb-4 border-b border-[#dce2f7]">Resumo da reserva</h2>
          <div className="space-y-4 py-3">
            <Summary label="Local">
              <div className="flex items-baseline justify-between">
                <span className="text-base font-bold text-[#141b2b]">{k.name}</span>
                <span className="text-xs text-[#40493d]">Até {k.capacity} pessoas</span>
              </div>
            </Summary>
            <Summary label="Data da utilização">
              <div className="flex items-center gap-1.5 text-[#141b2b] text-sm font-semibold">
                <Icon name="calendar_today" className="text-[18px] text-[#0d631b]" />
                <span>{formatDate(date)}</span>
                <span className="text-xs text-[#40493d] font-normal">({weekday(date)})</span>
              </div>
            </Summary>
            <Summary label="Horário reservado">
              <div className="flex items-center gap-1.5 text-[#141b2b] text-sm font-semibold">
                <Icon name="schedule" className="text-[18px] text-[#0d631b]" />
                <span>08:00 às 22:00</span>
              </div>
            </Summary>
            <Summary label="Itens inclusos">
              <ul className="text-xs text-[#40493d] space-y-1 mt-1">
                {k.items.map((item) => (
                  <li key={item} className="flex items-center gap-1.5">
                    <Icon name="check_circle" className="text-[14px] text-[#0d631b]" />
                    {item}
                  </li>
                ))}
              </ul>
            </Summary>
          </div>
          <div className="my-4 h-[1px] bg-[#dce2f7]"></div>
          <div className="flex items-baseline justify-between mb-4">
            <span className="text-sm text-[#141b2b] font-medium">Valor total</span>
            <span className="text-2xl font-bold text-[#2e7d32]">{formatCurrency(k.dailyRate)}</span>
          </div>
          <div className="bg-[#ffffff] p-3 rounded-lg border border-[#dce2f7] flex items-start gap-2">
            <Icon name="info" className="text-[18px] text-[#0d631b] mt-0.5 shrink-0" />
            <p className="text-xs text-[#40493d] leading-snug">Cancelamento gratuito até 48 horas antes da data.</p>
          </div>
        </aside>
      </div>
    </div>
  );
}

function Field({ label, htmlFor, hint, children }: { label: string; htmlFor: string; hint?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <label className="text-sm font-semibold text-[#141b2b]" htmlFor={htmlFor}>
        {label} <span className="text-[#ba1a1a]">*</span>
      </label>
      {children}
      {hint && <span className="text-xs text-[#40493d]">{hint}</span>}
    </div>
  );
}

function Summary({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs uppercase tracking-wider text-[#40493d] font-semibold">{label}</span>
      {children}
    </div>
  );
}
