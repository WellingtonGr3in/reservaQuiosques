import { useState, type FormEvent, type ReactNode } from 'react';
import { useNavigate, useSearchParams } from 'react-router';
import { api, ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorBox } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCPF } from '../lib/format';

const inputClass =
  'w-full h-10 px-3 bg-[#f1f3ff] text-[#141b2b] text-sm border border-[#dce2f7] rounded outline-none focus:bg-[#ffffff] focus:border-[#2e7d32] transition-colors';

// Tela "Entrar / Criar Conta" (HU05 / RF09).
// RN09: a conta é criada com o CPF de quem já fez reservas; o e-mail é usado só para entrar.
export function LoginPage() {
  const [searchParams] = useSearchParams();
  const [tab, setTab] = useState<'login' | 'register'>(searchParams.get('aba') === 'criar' ? 'register' : 'login');
  const { signIn } = useAuth();
  const navigate = useNavigate();

  const [login, setLogin] = useState({ email: '', password: '' });
  const [register, setRegister] = useState({ cpf: '', email: '', password: '' });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async (e: FormEvent, action: () => Promise<{ token: string; expiresAt: string }>) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      signIn(await action());
      navigate('/minhas-reservas');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
      setBusy(false);
    }
  };

  const switchTab = (next: 'login' | 'register') => {
    setTab(next);
    setError(null);
  };

  return (
    <div className="min-h-[calc(100vh-8rem)] bg-[#f1f3ff]/40 flex items-center justify-center p-6">
      <div className="w-full max-w-md bg-[#ffffff] border border-[#dce2f7] rounded-xl p-8 shadow-sm">
        <div className="flex items-center gap-3 mb-6 bg-[#f1f3ff] rounded-lg p-3 border border-[#dce2f7]">
          <div className="w-8 h-8 rounded-lg bg-[#2e7d32] text-white flex items-center justify-center">
            <Icon name="outdoor_grill" className="text-[18px]" />
          </div>
          <div>
            <span className="block text-sm font-bold text-[#141b2b] leading-tight">ReservaQuiosque</span>
            <span className="block text-xs text-[#40493d]">Gestão e Agendamento Integrado</span>
          </div>
        </div>

        <div className="mb-6">
          <h1 className="text-2xl font-bold text-[#141b2b]">{tab === 'login' ? 'Acesse sua conta' : 'Crie sua conta'}</h1>
          <p className="text-xs text-[#40493d] mt-1">
            {tab === 'login'
              ? 'Entre para acompanhar e gerenciar suas reservas.'
              : 'Use o CPF das suas reservas e escolha um e-mail e uma senha para entrar. As reservas feitas antes com esse CPF aparecem no seu histórico.'}
          </p>
        </div>

        <div className="grid grid-cols-2 p-1 bg-[#e1e8fd] rounded-lg mb-6 gap-1 border border-[#dce2f7]" role="tablist">
          <TabButton active={tab === 'login'} onClick={() => switchTab('login')}>
            Entrar
          </TabButton>
          <TabButton active={tab === 'register'} onClick={() => switchTab('register')}>
            Criar conta
          </TabButton>
        </div>

        {tab === 'login' ? (
          <form onSubmit={(e) => run(e, () => api.login(login.email, login.password))} className="flex flex-col gap-4">
            <Field label="E-mail" htmlFor="login-email">
              <input
                id="login-email"
                type="email"
                required
                autoComplete="email"
                value={login.email}
                onChange={(e) => setLogin({ ...login, email: e.target.value })}
                placeholder="seu@email.com"
                className={inputClass}
              />
            </Field>
            <Field label="Senha" htmlFor="login-password">
              <input
                id="login-password"
                type="password"
                required
                autoComplete="current-password"
                value={login.password}
                onChange={(e) => setLogin({ ...login, password: e.target.value })}
                className={inputClass}
              />
            </Field>
            {error && <ErrorBox message={error} />}
            <SubmitButton busy={busy} label="Entrar" icon="arrow_forward" />
          </form>
        ) : (
          <form onSubmit={(e) => run(e, () => api.createAccount(register))} className="flex flex-col gap-3">
            <Field label="CPF" htmlFor="register-cpf">
              <input
                id="register-cpf"
                required
                inputMode="numeric"
                maxLength={14}
                value={register.cpf}
                onChange={(e) => setRegister({ ...register, cpf: formatCPF(e.target.value) })}
                placeholder="000.000.000-00"
                className={inputClass}
              />
            </Field>
            <Field label="E-mail para entrar" htmlFor="register-email">
              <input
                id="register-email"
                type="email"
                required
                autoComplete="email"
                value={register.email}
                onChange={(e) => setRegister({ ...register, email: e.target.value })}
                placeholder="seu@email.com"
                className={inputClass}
              />
            </Field>
            <Field label="Senha" htmlFor="register-password">
              <input
                id="register-password"
                type="password"
                required
                minLength={8}
                maxLength={72}
                autoComplete="new-password"
                value={register.password}
                onChange={(e) => setRegister({ ...register, password: e.target.value })}
                placeholder="Mínimo de 8 caracteres"
                className={inputClass}
              />
            </Field>
            {error && <ErrorBox message={error} />}
            <SubmitButton busy={busy} label="Criar conta" icon="check" />
          </form>
        )}
      </div>
    </div>
  );
}

function TabButton({ active, onClick, children }: { active: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={active}
      onClick={onClick}
      className={`py-2 px-4 text-center text-xs font-semibold rounded transition-colors ${
        active ? 'bg-[#ffffff] text-[#2e7d32] shadow-sm font-bold' : 'text-[#40493d] hover:text-[#141b2b]'
      }`}
    >
      {children}
    </button>
  );
}

function Field({ label, htmlFor, children }: { label: string; htmlFor: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <label className="text-xs font-semibold text-[#40493d]" htmlFor={htmlFor}>
        {label}
      </label>
      {children}
    </div>
  );
}

function SubmitButton({ busy, label, icon }: { busy: boolean; label: string; icon: string }) {
  return (
    <button
      type="submit"
      disabled={busy}
      className="w-full h-10 mt-1 bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-bold rounded transition-colors flex items-center justify-center gap-2 cursor-pointer shadow-sm disabled:opacity-70"
    >
      <span>{busy ? 'Aguarde...' : label}</span>
      <Icon name={icon} className="text-[18px]" />
    </button>
  );
}
