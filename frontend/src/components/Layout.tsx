import { NavLink, Outlet, useNavigate } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { Icon } from './Icon';

function navClass({ isActive }: { isActive: boolean }) {
  return `transition-colors py-1 px-1.5 sm:px-2 rounded hover:text-[#141b2b] whitespace-nowrap ${
    isActive ? 'text-[#0d631b] font-bold border-b-2 border-[#0d631b]' : 'text-[#40493d]'
  }`;
}

export function Layout() {
  const { isLoggedIn, signOut } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-[#ffffff] text-[#141b2b] flex flex-col justify-between selection:bg-[#acf4a4] selection:text-[#0d631b]">
      <header className="sticky top-0 z-40 w-full bg-[#ffffff] border-b border-[#dce2f7]/60 print:hidden">
        <div className="h-16 max-w-[1100px] mx-auto px-4 sm:px-6 flex items-center justify-between gap-2">
          <NavLink to="/" className="font-bold text-base sm:text-lg text-[#141b2b] hover:text-[#0d631b] transition-colors tracking-tight whitespace-nowrap">
            <span className="sm:hidden">Quiosques</span>
            <span className="hidden sm:inline">Reserva de Quiosques</span>
          </NavLink>

          <div className="flex items-center gap-2 sm:gap-6">
            <nav className="flex items-center gap-1 sm:gap-4 text-xs sm:text-sm font-semibold">
              <NavLink to="/consultar" className={navClass}>
                <span className="inline sm:hidden">Consultar</span>
                <span className="hidden sm:inline">Consultar reserva</span>
              </NavLink>
              <NavLink to="/minhas-reservas" className={navClass}>
                <span className="inline sm:hidden">Reservas</span>
                <span className="hidden sm:inline">Minhas reservas</span>
              </NavLink>
              {isLoggedIn ? (
                <button
                  type="button"
                  onClick={() => {
                    signOut();
                    navigate('/');
                  }}
                  className="text-[#40493d] hover:text-[#141b2b] py-1 px-1.5 sm:px-2 whitespace-nowrap"
                >
                  Sair
                </button>
              ) : (
                <NavLink to="/entrar" className={navClass}>
                  Entrar
                </NavLink>
              )}
            </nav>

            <NavLink
              to={isLoggedIn ? '/minhas-reservas' : '/entrar'}
              title={isLoggedIn ? 'Minhas reservas' : 'Entrar'}
              className="w-8 h-8 rounded-full bg-[#0d631b] hidden sm:flex items-center justify-center text-white hover:bg-[#2e7d32] transition-colors shrink-0"
            >
              <Icon name="person" className="text-[18px]" />
            </NavLink>
          </div>
        </div>
      </header>

      <main className="w-full flex-1">
        <Outlet />
      </main>

      <footer className="w-full bg-[#ffffff] border-t border-[#dce2f7]/60 py-6 mt-12 print:hidden">
        <div className="max-w-[1100px] mx-auto px-6 flex flex-col md:flex-row items-center justify-between gap-2 text-center md:text-left text-xs text-[#40493d]">
          <p>© 2026 Reserva de Quiosques • Sistema de agendamento de quiosques e churrasqueiras</p>
          <div className="flex items-center gap-4">
            <NavLink to="/admin" className="hover:text-[#0d631b] hover:underline">
              Administração
            </NavLink>
            <span>Português (Brasil)</span>
          </div>
        </div>
      </footer>
    </div>
  );
}
