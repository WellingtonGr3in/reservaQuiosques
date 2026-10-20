import { useEffect, useState } from 'react';
import { Link, Navigate, useNavigate, useParams } from 'react-router';
import { api, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { CancelModal } from '../components/CancelModal';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { ReservationDetailsCard } from '../components/ReservationDetailsCard';
import { useApi } from '../lib/useApi';

// "Minhas reservas" > "Ver comprovante" / "Ver detalhes". RN07 também vale com login.
export function MyReservationDetailPage() {
  const { code = '' } = useParams();
  const { isLoggedIn, signOut } = useAuth();
  const navigate = useNavigate();
  const reservation = useApi(() => (isLoggedIn ? api.myReservation(code) : Promise.resolve(null)), [code, isLoggedIn]);

  const [cancelling, setCancelling] = useState(false);
  const [cancelBusy, setCancelBusy] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);

  useEffect(() => {
    if (reservation.error?.status === 401) {
      signOut();
      navigate('/entrar', { replace: true });
    }
  }, [reservation.error, signOut, navigate]);

  if (!isLoggedIn) return <Navigate to="/entrar" replace />;
  if (reservation.loading) return <Loading />;
  if (reservation.error || !reservation.data) {
    return (
      <div className="max-w-[860px] mx-auto px-6 py-8">
        <ErrorBox message={reservation.error?.message ?? 'Reserva não encontrada'} />
      </div>
    );
  }

  const cancel = async () => {
    setCancelBusy(true);
    setCancelError(null);
    try {
      reservation.setData(await api.cancelMine(code));
      setCancelling(false);
    } catch (err) {
      setCancelError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setCancelBusy(false);
    }
  };

  return (
    <div className="max-w-[860px] mx-auto px-6 py-8 flex flex-col gap-4">
      <Link to="/minhas-reservas" className="flex items-center gap-1 text-xs text-[#40493d] hover:text-[#0d631b] print:hidden">
        <Icon name="arrow_back" className="text-[16px]" />
        <span>Minhas reservas</span>
      </Link>
      <ReservationDetailsCard reservation={reservation.data} onCancel={() => setCancelling(true)} />
      {cancelling && (
        <CancelModal
          reservation={reservation.data}
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
