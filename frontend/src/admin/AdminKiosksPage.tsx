import { useState } from 'react';
import { Link } from 'react-router';
import { adminApi, ApiError } from '../api/client';
import type { AdminKiosk } from '../api/types';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCurrency } from '../lib/format';
import { useApi } from '../lib/useApi';
import { useAdminSessionGuard } from './AdminLayout';
import { dangerButton, PageHeader, primaryButton, secondaryButton } from './ui';

// RF16: todos os quiosques, inclusive desativados. RN15: só exclui quem nunca teve reservas.
export function AdminKiosksPage() {
  const kiosks = useApi(() => adminApi.listKiosks(), []);
  useAdminSessionGuard(kiosks.error?.status);
  const [deleting, setDeleting] = useState<AdminKiosk | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const remove = async () => {
    if (!deleting) return;
    setBusy(true);
    setError(null);
    try {
      await adminApi.deleteKiosk(deleting.id);
      setDeleting(null);
      kiosks.reload();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="max-w-[1100px] mx-auto px-4 sm:px-6 py-8">
      <PageHeader
        title="Quiosques"
        subtitle="Cadastre, edite e desative os quiosques exibidos para os clientes."
        actions={
          <Link to="/admin/quiosques/novo" className={primaryButton}>
            <Icon name="add" className="text-[20px]" />
            Novo quiosque
          </Link>
        }
      />

      {kiosks.loading && <Loading />}
      {kiosks.error && <ErrorBox message={kiosks.error.message} />}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {kiosks.data?.map((k) => (
          <article key={k.id} className={`bg-[#ffffff] rounded-xl overflow-hidden border border-[#dce2f7] flex flex-col ${k.isActive ? '' : 'opacity-70'}`}>
            <div className="relative h-36 bg-[#e9edff]">
              <img src={k.photoUrl} alt={k.name} className="w-full h-full object-cover" />
              <span
                className={`absolute top-2 left-2 px-2 py-0.5 rounded text-xs font-bold ${
                  k.isActive ? 'bg-[#acf4a4] text-[#307231]' : 'bg-[#e1e8fd] text-[#40493d]'
                }`}
              >
                {k.isActive ? 'Ativo' : 'Desativado'}
              </span>
            </div>
            <div className="p-4 flex flex-col gap-1 flex-1">
              <h2 className="font-bold">{k.name}</h2>
              <p className="text-xs text-[#40493d]">
                Até {k.capacity} pessoas • {formatCurrency(k.dailyRate)} / dia
              </p>
              <p className="text-xs text-[#707a6c]">{k.items.length} itens • {k.photos.length} fotos</p>
            </div>
            <div className="p-4 pt-0 flex gap-2">
              <Link to={`/admin/quiosques/${k.id}`} className={`${secondaryButton} flex-1`}>
                <Icon name="edit" className="text-[18px]" /> Editar
              </Link>
              <button
                type="button"
                title={k.hasReservations ? 'Tem reservas: desative em vez de excluir' : 'Excluir'}
                disabled={k.hasReservations}
                onClick={() => {
                  setError(null);
                  setDeleting(k);
                }}
                className={`${secondaryButton} text-[#ba1a1a]`}
              >
                <Icon name="delete" className="text-[18px]" />
              </button>
            </div>
          </article>
        ))}
      </div>

      {deleting && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#293040]/40 backdrop-blur-sm p-4" role="dialog" aria-modal="true">
          <div className="w-full max-w-md bg-[#ffffff] rounded-xl shadow-xl border border-[#dce2f7] p-6 flex flex-col gap-4">
            <h3 className="text-lg font-bold">Excluir {deleting.name}?</h3>
            <p className="text-sm text-[#40493d]">O quiosque, suas fotos e itens serão apagados. Isso não pode ser desfeito.</p>
            {error && <ErrorBox message={error} />}
            <div className="flex justify-end gap-2">
              <button type="button" disabled={busy} onClick={() => setDeleting(null)} className={secondaryButton}>
                Voltar
              </button>
              <button type="button" disabled={busy} onClick={remove} className={dangerButton}>
                {busy ? 'Excluindo...' : 'Excluir'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
