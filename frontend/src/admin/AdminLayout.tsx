import { useEffect } from 'react';
import { Navigate, NavLink, Outlet, useNavigate } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { Icon } from '../components/Icon';

function navClass({ isActive }: { isActive: boolean }) {
  return `transition-colors py-1 px-2 rounded whitespace-nowrap ${
    isActive ? 'text-[#0d631b] font-bold border-b-2 border-[#0d631b]' : 'text-[#40493d] hover:text-[#141b2b]'
  }`;
}

// Área de administração (RF11): só com login de administrador.
export function AdminLayout() {
  const { isAdmin, signOutAdmin } = useAuth();
  const navigate = useNavigate();

  if (!isAdmin) return <Navigate to="/admin/entrar" replace />;

  return (
    <div className="min-h-screen bg-[#f9f9ff] text-[#141b2b] flex flex-col">
      <header className="sticky top-0 z-40 w-full bg-[#141b2b] text-white print:hidden">
        <div className="h-14 max-w-[1100px] mx-auto px-4 sm:px-6 flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 font-bold text-sm sm:text-base whitespace-nowrap">
            <Icon name="admin_panel_settings" className="text-[20px] text-[#acf4a4]" />
            <span className="hidden sm:inline">Administração</span>
          </div>
          <nav className="flex items-center gap-2 sm:gap-4 text-xs sm:text-sm font-semibold bg-white rounded-lg px-2 py-1">
            <NavLink to="/admin/reservas" className={navClass}>
              Reservas
            </NavLink>
            <NavLink to="/admin/quiosques" className={navClass}>
              Quiosques
            </NavLink>
          </nav>
          <button
            type="button"
            onClick={() => {
              signOutAdmin();
              navigate('/admin/entrar');
            }}
            className="text-xs sm:text-sm text-white/80 hover:text-white whitespace-nowrap"
          >
            Sair
          </button>
        </div>
      </header>
      <main className="w-full flex-1">
        <Outlet />
      </main>
    </div>
  );
}

// Token de administrador vencido (60 min, RNF04): volta para o login.
export function useAdminSessionGuard(status: number | undefined) {
  const { signOutAdmin } = useAuth();
  const navigate = useNavigate();
  useEffect(() => {
    if (status === 401 || status === 403) {
      signOutAdmin();
      navigate('/admin/entrar', { replace: true });
    }
  }, [status, signOutAdmin, navigate]);
}
