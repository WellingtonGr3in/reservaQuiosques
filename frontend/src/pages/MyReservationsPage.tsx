import { useEffect, useState } from 'react';
import { Link, Navigate, useNavigate } from 'react-router';
import { api, ApiError } from '../api/client';
import type { ReservationDetails } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { CancelModal } from '../components/CancelModal';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { StatusBadge } from '../components/StatusBadge';
import { formatDate, parseDate, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';

// Tela "Minhas Reservas" (HU05 / RF10): histórico do cliente logado, incluindo as reservas
// feitas antes de criar a conta (RN09).
export function MyReservationsPage() {
  const { isLoggedIn, signOut } = useAuth();
  const navigate = useNavigate();
  const mine = useApi(() => (isLoggedIn ? api.myReservations() : Promise.resolve(null)), [isLoggedIn]);

  const [cancelling, setCancelling] = useState<ReservationDetails | null>(null);
  const [cancelBusy, setCancelBusy] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  // Token vencido (RNF04: 60 minutos): volta para o login.
  useEffect(() => {
    if (mine.error?.status === 401) {
      signOut();
      navigate('/entrar', { replace: true });
    }
  }, [mine.error, signOut, navigate]);

  if (!isLoggedIn) return <Navigate to="/entrar" replace />;
  if (mine.loading) return <Loading />;
  if (mine.error || !mine.data) {
    return (
      <div className="max-w-[1100px] mx-auto px-6 py-8">
        <ErrorBox message={mine.error?.message ?? 'Não foi possível carregar suas reservas.'} />
      </div>
    );
  }

  const { reservations, maskedCpf } = mine.data;
  const thisYear = new Date().getFullYear();
  const active = reservations.filter((r) => r.status === 'CONFIRMADA' || r.status === 'PENDENTE_PAGAMENTO').length;
  const completed = reservations.filter((r) => r.status === 'REALIZADA' && parseDate(r.date).getFullYear() === thisYear).length;
  const cancelled = reservations.filter((r) => r.status === 'CANCELADA').length;

  const cancel = async () => {
    if (!cancelling) return;
    setCancelBusy(true);
    setCancelError(null);
    try {
      await api.cancelMine(cancelling.code);
      setCancelling(null);
      mine.reload();
    } catch (err) {
      setCancelError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setCancelBusy(false);
    }
  };

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8">
      <section className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-1.5 text-[#0d631b] text-xs font-semibold uppercase tracking-wider">
            <Icon name="verified_user" className="text-[16px]" />
            <span>Área do Condômino</span>
          </div>
          <h1 className="text-2xl font-bold text-[#141b2b] tracking-tight">Minhas reservas</h1>
          <p className="text-xs text-[#40493d]">
            Histórico de reservas vinculadas ao CPF{' '}
            <span className="font-mono text-xs font-medium text-[#141b2b] bg-[#e1e8fd] px-2 py-0.5 rounded border border-[#dce2f7]">
              {maskedCpf}
            </span>
          </p>
        </div>
        <Link
          to="/"
          className="inline-flex items-center gap-2 bg-[#2e7d32] text-white text-xs font-bold px-4 py-2.5 rounded shadow-sm hover:bg-[#0d631b] transition-colors self-start sm:self-auto"
        >
          <Icon name="add" className="text-[20px]" />
          <span>Nova reserva</span>
        </Link>
      </section>

      <section className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <Metric label="Ativas no momento" value={active} icon="event_available" color="bg-[#acf4a4] text-[#307231]" />
        <Metric label="Concluídas este ano" value={completed} icon="task_alt" color="bg-[#e9edff] text-[#40493d]" />
        <Metric label="Canceladas" value={cancelled} icon="event_busy" color="bg-[#ffdad6] text-[#93000a]" />
      </section>

      <section className="bg-[#ffffff] rounded-xl shadow-sm border border-[#dce2f7] overflow-hidden mb-8">
        {reservations.length === 0 ? (
          <p className="p-8 text-center text-sm text-[#40493d]">Você ainda não tem reservas.</p>
        ) : (
          <div className="overflow-x-auto w-full">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-[#f1f3ff] text-[#40493d] text-xs font-semibold border-b border-[#dce2f7]">
                  <th className="py-3.5 px-6" scope="col">Quiosque</th>
                  <th className="py-3.5 px-4" scope="col">Data</th>
                  <th className="py-3.5 px-4" scope="col">Código</th>
                  <th className="py-3.5 px-4" scope="col">Status</th>
                  <th className="py-3.5 px-6 text-right" scope="col">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#dce2f7]/50 text-sm">
                {reservations.map((r) => (
                  <tr key={r.code} className="hover:bg-[#f1f3ff]/40 transition-colors">
                    <td className="py-4 px-6">
                      <div className="flex items-center gap-3">
                        <img src={r.kioskPhotoUrl} alt="" className="w-10 h-10 rounded-lg object-cover flex-shrink-0" />
                        <div className="flex flex-col">
                          <span className="font-semibold text-[#141b2b]">{r.kioskName}</span>
                          <span className="text-xs text-[#40493d]">Capacidade até {r.kioskCapacity} pessoas</span>
                        </div>
                      </div>
                    </td>
                    <td className="py-4 px-4 whitespace-nowrap">
                      <div className="flex flex-col">
                        <span className="font-semibold text-[#141b2b]">{formatDate(r.date)}</span>
                        <span className="text-xs text-[#40493d]">{weekday(r.date)} • 08h às 22h</span>
                      </div>
                    </td>
                    <td className="py-4 px-4 whitespace-nowrap">
                      <span className="font-mono text-xs font-semibold text-[#141b2b] bg-[#e9edff] border border-[#dce2f7] px-2.5 py-1 rounded tracking-wider select-all">
                        {r.code}
                      </span>
                    </td>
                    <td className="py-4 px-4">
                      <StatusBadge status={r.status} />
                    </td>
                    <td className="py-4 px-6 whitespace-nowrap text-right">
                      <div className="inline-flex items-center gap-2 justify-end">
                        <Link
                          to={`/minhas-reservas/${r.code}`}
                          className="inline-flex items-center gap-1 text-[#0d631b] hover:text-[#2e7d32] text-xs font-semibold px-2 py-1.5 rounded hover:bg-[#f1f3ff] transition-colors"
                        >
                          <Icon name={r.status === 'CONFIRMADA' ? 'receipt_long' : 'visibility'} className="text-[18px]" />
                          <span>{r.status === 'CONFIRMADA' ? 'Ver comprovante' : 'Ver detalhes'}</span>
                        </Link>
                        {r.status === 'PENDENTE_PAGAMENTO' && (
                          <Link
                            to={`/reservas/${r.code}/pagamento`}
                            className="inline-flex items-center gap-1 text-[#0d631b] hover:text-[#2e7d32] text-xs font-semibold px-2 py-1.5 rounded hover:bg-[#f1f3ff] transition-colors"
                          >
                            <Icon name="qr_code_2" className="text-[18px]" />
                            <span>Pagar</span>
                          </Link>
                        )}
                        {r.canCancel && (
                          <button
                            type="button"
                            onClick={() => setCancelling(r)}
                            className="inline-flex items-center gap-1 text-[#ba1a1a] hover:bg-[#ffdad6] hover:text-[#93000a] text-xs font-semibold px-2 py-1.5 rounded transition-colors cursor-pointer"
                          >
                            <Icon name="close" className="text-[18px]" />
                            <span>Cancelar</span>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

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

function Metric({ label, value, icon, color }: { label: string; value: number; icon: string; color: string }) {
  return (
    <div className="bg-[#ffffff] p-4 rounded-xl shadow-sm border border-[#dce2f7] flex items-center justify-between">
      <div className="flex flex-col">
        <span className="text-xs text-[#40493d] uppercase tracking-wider font-semibold">{label}</span>
        <span className="text-2xl text-[#141b2b] font-bold mt-1">{String(value).padStart(2, '0')}</span>
      </div>
      <div className={`w-10 h-10 rounded-full flex items-center justify-center ${color}`}>
        <Icon name={icon} className="text-[22px]" />
      </div>
    </div>
  );
}
