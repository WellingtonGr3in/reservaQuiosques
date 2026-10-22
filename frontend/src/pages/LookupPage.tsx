import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { api, ApiError } from '../api/client';
import type { MyReservations, ReservationDetails } from '../api/types';
import { CancelModal } from '../components/CancelModal';
import { ErrorBox, InfoBox } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { ReservationDetailsCard } from '../components/ReservationDetailsCard';
import { rememberBooking } from '../lib/bookingSession';
import { formatCPF } from '../lib/format';

const inputClass =
  'w-full h-11 pl-10 pr-3 rounded-lg bg-[#f1f3ff] text-[#141b2b] font-mono text-sm border border-[#dce2f7] focus:bg-[#ffffff] focus:border-[#2e7d32] outline-none transition-colors';

// Tela "Consulta de Reserva" (HU04 / RF08): sem login, só com o CPF do titular (RN10).
// Mostra todas as reservas do CPF, cada uma com a opção de cancelar (RN07) ou pagar, se ainda aguarda o Pix.
// A API limita a 5 tentativas por minuto (RNF06) e devolve a mensagem para exibir.
export function LookupPage() {
  const [cpf, setCpf] = useState('');
  const [searchedCpf, setSearchedCpf] = useState('');
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<MyReservations | null>(null);

  const [cancelling, setCancelling] = useState<ReservationDetails | null>(null);
  const [cancelBusy, setCancelBusy] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const search = async (e: FormEvent) => {
    e.preventDefault();
    setSearching(true);
    setError(null);
    setNotice(null);
    setResult(null);
    try {
      setResult(await api.lookup(cpf));
      setSearchedCpf(cpf);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setSearching(false);
    }
  };

  const cancel = async () => {
    if (!cancelling || !result) return;
    setCancelBusy(true);
    setCancelError(null);
    try {
      const updated = await api.cancel(cancelling.code, searchedCpf);
      setResult({ ...result, reservations: result.reservations.map((r) => (r.code === updated.code ? updated : r)) });
      setNotice(`Reserva ${updated.code} cancelada.`);
      setCancelling(null);
    } catch (err) {
      setCancelError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setCancelBusy(false);
    }
  };

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8 flex flex-col items-center">
      <div className="w-full max-w-[620px] mb-8 print:hidden">
        <div className="bg-[#ffffff] p-6 md:p-8 rounded-xl shadow-sm border border-[#dce2f7]">
          <h1 className="text-2xl font-bold text-[#141b2b] mb-1">Consultar reserva</h1>
          <p className="text-sm text-[#40493d] mb-6">Informe o CPF do titular para ver suas reservas ou cancelar.</p>

          <form onSubmit={search} className="flex flex-col gap-4">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold text-[#141b2b]" htmlFor="cpf-titular">
                CPF do titular
              </label>
              <div className="relative flex items-center">
                <Icon name="badge" className="absolute left-3 text-[#707a6c] text-[20px] pointer-events-none" />
                <input
                  id="cpf-titular"
                  required
                  inputMode="numeric"
                  minLength={14}
                  maxLength={14}
                  value={cpf}
                  onChange={(e) => setCpf(formatCPF(e.target.value))}
                  placeholder="000.000.000-00"
                  title="Informe os 11 dígitos do CPF"
                  className={inputClass}
                />
              </div>
            </div>

            {error && <ErrorBox message={error} />}

            <button
              type="submit"
              disabled={searching}
              className="w-full h-11 mt-1 bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-semibold rounded-lg flex items-center justify-center gap-2 transition-all shadow-sm cursor-pointer disabled:opacity-70"
            >
              <Icon name="search" className="text-[20px]" />
              <span>{searching ? 'Consultando...' : 'Consultar'}</span>
            </button>
          </form>
        </div>
      </div>

      {result && (
        <div className="w-full max-w-[860px] flex flex-col gap-6">
          <div className="flex flex-col sm:flex-row sm:items-baseline sm:justify-between gap-1">
            <h2 className="text-lg font-bold text-[#141b2b]">
              {result.reservations.length === 1 ? '1 reserva encontrada' : `${result.reservations.length} reservas encontradas`}
            </h2>
            <p className="text-xs text-[#40493d]">
              Titular: <span className="font-semibold text-[#141b2b]">{result.holderName}</span> •{' '}
              <span className="font-mono">{result.maskedCpf}</span>
            </p>
          </div>

          {notice && <InfoBox message={notice} />}

          {result.reservations.map((r) => (
            <div key={r.code} className="flex flex-col gap-2">
              <ReservationDetailsCard reservation={r} onCancel={() => setCancelling(r)} />
              {r.status === 'PENDENTE_PAGAMENTO' && (
                <Link
                  to={`/reservas/${r.code}/pagamento`}
                  onClick={() => rememberBooking(r.code, searchedCpf)}
                  className="self-end inline-flex items-center gap-1.5 bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-semibold px-4 py-2 rounded-lg shadow-sm transition-colors"
                >
                  <Icon name="qr_code_2" className="text-[18px]" />
                  <span>Pagar reserva {r.code}</span>
                </Link>
              )}
            </div>
          ))}
        </div>
      )}

      {cancelling && (
        <CancelModal
          reservation={cancelling}
          busy={cancelBusy}
          error={cancelError}
          onConfirm={cancel}
          onClose={() => {
            setCancelling(null);
            setCancelError(null);
          }}
        />
      )}
    </div>
  );
}
