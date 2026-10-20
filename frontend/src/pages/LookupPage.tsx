import { useState, type FormEvent } from 'react';
import { api, ApiError } from '../api/client';
import type { ReservationDetails } from '../api/types';
import { CancelModal } from '../components/CancelModal';
import { ErrorBox } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { ReservationDetailsCard } from '../components/ReservationDetailsCard';
import { formatCPF } from '../lib/format';

const inputClass =
  'w-full h-11 pl-10 pr-3 rounded-lg bg-[#f1f3ff] text-[#141b2b] font-mono text-sm border border-[#dce2f7] focus:bg-[#ffffff] focus:border-[#2e7d32] outline-none transition-colors';

// Tela "Consulta de Reserva" (HU04 / RF08): sem login, com código + CPF (RN10).
// A API limita a 5 tentativas por minuto (RNF06) e devolve a mensagem para exibir.
export function LookupPage() {
  const [code, setCode] = useState('');
  const [cpf, setCpf] = useState('');
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ReservationDetails | null>(null);

  const [cancelling, setCancelling] = useState(false);
  const [cancelBusy, setCancelBusy] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  const search = async (e: FormEvent) => {
    e.preventDefault();
    setSearching(true);
    setError(null);
    setResult(null);
    try {
      setResult(await api.lookup(code.trim(), cpf));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setSearching(false);
    }
  };

  const cancel = async () => {
    if (!result) return;
    setCancelBusy(true);
    setCancelError(null);
    try {
      setResult(await api.cancel(result.code, cpf));
      setCancelling(false);
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
          <div className="flex items-center gap-2 mb-1 text-[#0d631b]">
            <Icon name="manage_search" className="text-[24px]" />
            <span className="text-xs uppercase tracking-wider font-bold">Sem precisar de conta</span>
          </div>
          <h1 className="text-2xl font-bold text-[#141b2b] mb-1">Consultar reserva</h1>
          <p className="text-sm text-[#40493d] mb-6">Informe o código da reserva e o CPF do titular para ver os detalhes ou cancelar.</p>

          <form onSubmit={search} className="flex flex-col gap-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold text-[#141b2b]" htmlFor="codigo-reserva">
                  Código da reserva
                </label>
                <div className="relative flex items-center">
                  <Icon name="tag" className="absolute left-3 text-[#707a6c] text-[20px] pointer-events-none" />
                  <input
                    id="codigo-reserva"
                    required
                    maxLength={10}
                    value={code}
                    onChange={(e) => setCode(e.target.value.toUpperCase())}
                    placeholder="QSK-7F3K9Q"
                    className={`${inputClass} uppercase`}
                  />
                </div>
              </div>
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
                    maxLength={14}
                    value={cpf}
                    onChange={(e) => setCpf(formatCPF(e.target.value))}
                    placeholder="000.000.000-00"
                    className={inputClass}
                  />
                </div>
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
        <div className="w-full max-w-[860px]">
          <ReservationDetailsCard reservation={result} onCancel={() => setCancelling(true)} />
        </div>
      )}

      {cancelling && result && (
        <CancelModal
          reservation={result}
          busy={cancelBusy}
          error={cancelError}
          onConfirm={cancel}
          onClose={() => {
            setCancelling(false);
            setCancelError(null);
          }}
        />
      )}
    </div>
  );
}
