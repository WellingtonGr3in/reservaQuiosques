import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { adminApi, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorBox } from '../components/Feedback';
import { Icon } from '../components/Icon';

const inputClass =
  'w-full h-10 px-3 bg-[#f1f3ff] text-[#141b2b] text-sm border border-[#dce2f7] rounded outline-none focus:bg-[#ffffff] focus:border-[#2e7d32] transition-colors';

// RF11: login da área de administração.
export function AdminLoginPage() {
  const { isAdmin, signInAdmin } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (isAdmin) return <Navigate to="/admin/reservas" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      signInAdmin(await adminApi.login(email, password));
      navigate('/admin/reservas');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
      setBusy(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#141b2b] flex items-center justify-center p-6">
      <div className="w-full max-w-sm bg-[#ffffff] rounded-xl p-8 shadow-xl">
        <div className="flex items-center gap-2 mb-6 text-[#0d631b]">
          <Icon name="admin_panel_settings" className="text-[28px]" />
          <div>
            <h1 className="text-xl font-bold text-[#141b2b]">Administração</h1>
            <p className="text-xs text-[#40493d]">Reserva de Quiosques</p>
          </div>
        </div>
        <form onSubmit={submit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <label htmlFor="admin-email" className="text-xs font-semibold text-[#40493d]">
              Usuário
            </label>
            <input id="admin-email" type="text" required autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} className={inputClass} />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="admin-password" className="text-xs font-semibold text-[#40493d]">
              Senha
            </label>
            <input
              id="admin-password"
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className={inputClass}
            />
          </div>
          {error && <ErrorBox message={error} />}
          <button
            type="submit"
            disabled={busy}
            className="w-full h-10 bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-bold rounded transition-colors disabled:opacity-70 cursor-pointer"
          >
            {busy ? 'Entrando...' : 'Entrar'}
          </button>
        </form>
      </div>
    </div>
  );
}
