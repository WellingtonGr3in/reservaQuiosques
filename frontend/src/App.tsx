import { Navigate, Route, Routes } from 'react-router';
import { AdminKioskFormPage } from './admin/AdminKioskFormPage';
import { AdminKiosksPage } from './admin/AdminKiosksPage';
import { AdminLayout } from './admin/AdminLayout';
import { AdminLoginPage } from './admin/AdminLoginPage';
import { AdminNewReservationPage } from './admin/AdminNewReservationPage';
import { AdminReservationDetailPage } from './admin/AdminReservationDetailPage';
import { AdminReservationsPage } from './admin/AdminReservationsPage';
import { Layout } from './components/Layout';
import { CustomerDataPage } from './pages/CustomerDataPage';
import { KioskDetailPage } from './pages/KioskDetailPage';
import { KioskListPage } from './pages/KioskListPage';
import { LoginPage } from './pages/LoginPage';
import { LookupPage } from './pages/LookupPage';
import { MyReservationDetailPage } from './pages/MyReservationDetailPage';
import { MyReservationsPage } from './pages/MyReservationsPage';
import { PaymentPage } from './pages/PaymentPage';
import { ReceiptPage } from './pages/ReceiptPage';

// SPA (ADR-002): uma página só; a URL acompanha a tela para o F5 e o "voltar" funcionarem.
// Uma rota por tela do protótipo (docs/03-prototipos/prototipos.md).
export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<KioskListPage />} />
        <Route path="quiosques/:id" element={<KioskDetailPage />} />
        <Route path="quiosques/:id/reservar" element={<CustomerDataPage />} />
        <Route path="reservas/:code/pagamento" element={<PaymentPage />} />
        <Route path="reservas/:code/comprovante" element={<ReceiptPage />} />
        <Route path="consultar" element={<LookupPage />} />
        <Route path="entrar" element={<LoginPage />} />
        <Route path="minhas-reservas" element={<MyReservationsPage />} />
        <Route path="minhas-reservas/:code" element={<MyReservationDetailPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>

      {/* Área de administração (HU06, HU07), com layout e login próprios. */}
      <Route path="admin/entrar" element={<AdminLoginPage />} />
      <Route path="admin" element={<AdminLayout />}>
        <Route index element={<Navigate to="reservas" replace />} />
        <Route path="reservas" element={<AdminReservationsPage />} />
        <Route path="reservas/nova" element={<AdminNewReservationPage />} />
        <Route path="reservas/:code" element={<AdminReservationDetailPage />} />
        <Route path="quiosques" element={<AdminKiosksPage />} />
        <Route path="quiosques/novo" element={<AdminKioskFormPage />} />
        <Route path="quiosques/:id" element={<AdminKioskFormPage />} />
      </Route>
    </Routes>
  );
}
