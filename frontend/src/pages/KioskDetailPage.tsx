import { useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import { api } from '../api/client';
import { Calendar } from '../components/Calendar';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCurrency, formatDate, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';

// Tela "Detalhe do Quiosque" (HU01 / RF02, RF03): dados do quiosque e escolha do dia.
export function KioskDetailPage() {
  const { id = '' } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const kiosk = useApi(() => api.getKiosk(id), [id]);
  const [selected, setSelected] = useState<string | null>(searchParams.get('data'));

  if (kiosk.loading) return <Loading />;
  if (kiosk.error || !kiosk.data) {
    return (
      <div className="max-w-[1100px] mx-auto px-6 py-8">
        <ErrorBox message={kiosk.error?.message ?? 'Quiosque não encontrado'}>
          <Link to="/" className="underline">
            Ver todos os quiosques
          </Link>
        </ErrorBox>
      </div>
    );
  }

  const k = kiosk.data;
  const gallery = k.photos.length > 0 ? k.photos : [k.photoUrl];

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8">
      <nav className="flex items-center gap-2 mb-6 text-[#40493d] text-xs">
        <Link to="/" className="hover:text-[#0d631b] transition-colors flex items-center gap-1">
          <Icon name="arrow_back" className="text-[16px]" />
          <span>Todos os quiosques</span>
        </Link>
        <span>/</span>
        <span className="text-[#141b2b] font-semibold">{k.name}</span>
      </nav>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        <section className="lg:col-span-7 flex flex-col gap-6">
          <div className="relative w-full h-[340px] md:h-[400px] rounded-xl overflow-hidden shadow-sm bg-[#e9edff] border border-[#dce2f7]">
            <img className="w-full h-full object-cover" src={gallery[0]} alt={k.name} />
            <div className="absolute top-4 left-4">
              <span className="inline-flex items-center gap-1 px-3 py-1 rounded bg-[#acf4a4] text-[#307231] text-xs font-bold tracking-wide shadow-sm">
                <Icon name="check_circle" className="text-[14px]" />
                ATIVO • PRONTO PARA USO
              </span>
            </div>
          </div>
          {gallery.length > 1 && (
            <div className="grid grid-cols-4 gap-2">
              {gallery.slice(1).map((url) => (
                <img key={url} src={url} alt={k.name} className="h-20 w-full rounded-lg object-cover border border-[#dce2f7]" />
              ))}
            </div>
          )}

          <div className="flex flex-col gap-2">
            <div className="flex flex-col sm:flex-row sm:items-baseline sm:justify-between gap-2">
              <h1 className="text-3xl font-bold text-[#141b2b] tracking-tight">{k.name}</h1>
              <div className="flex items-baseline gap-1">
                <span className="text-2xl font-bold text-[#2e7d32]">{formatCurrency(k.dailyRate)}</span>
                <span className="text-xs text-[#40493d]">/ dia</span>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-4 text-[#40493d] text-xs">
              <Chip icon="group" text={`Capacidade: ${k.capacity} pessoas`} />
              <Chip icon="schedule" text="08:00 às 22:00" />
            </div>
          </div>

          <p className="text-base text-[#40493d] leading-relaxed">{k.description}</p>

          <div className="bg-[#f1f3ff] p-5 rounded-xl border border-[#dce2f7]">
            <h2 className="text-lg font-semibold text-[#141b2b] mb-4 flex items-center gap-2">
              <Icon name="checklist" className="text-[#2e7d32] text-[20px]" />
              Itens inclusos
            </h2>
            <ul className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-sm text-[#141b2b]">
              {k.items.map((item) => (
                <li key={item} className="flex items-center gap-2">
                  <Icon name="check_circle" className="text-[#2e7d32] text-[20px]" />
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="flex items-start gap-3 p-4 rounded-xl bg-[#e9edff]/50 border border-[#dce2f7] text-[#40493d]">
            <Icon name="policy" className="text-[#2e7d32] text-[20px] mt-0.5" />
            <div className="text-xs leading-relaxed">
              <p className="font-semibold text-[#141b2b] mb-0.5">Regras de uso</p>
              <p>{k.usageRules}</p>
            </div>
          </div>
        </section>

        <aside className="lg:col-span-5 flex flex-col gap-5 lg:sticky lg:top-24">
          <div className="bg-[#ffffff] rounded-xl p-6 shadow-sm border border-[#dce2f7] flex flex-col gap-6">
            <Calendar kioskId={k.id} selected={selected} onSelect={setSelected} />

            <div className="p-4 rounded-lg bg-[#e9edff] flex flex-col gap-2 border border-[#dce2f7]">
              <span className="text-xs font-semibold text-[#40493d] uppercase tracking-wider">Resumo da reserva</span>
              <div className="flex items-start gap-2">
                <Icon name="event" className="text-[#2e7d32] text-[20px] shrink-0 mt-0.5" />
                {selected ? (
                  <div>
                    <p className="text-sm text-[#141b2b] font-bold">
                      {formatDate(selected)} ({weekday(selected)})
                    </p>
                    <p className="text-xs text-[#40493d]">Diária completa: 08:00 às 22:00</p>
                  </div>
                ) : (
                  <p className="text-sm text-[#40493d]">Escolha um dia disponível no calendário.</p>
                )}
              </div>
              <div className="flex justify-between items-center pt-2 mt-1 border-t border-[#dce2f7]">
                <span className="text-xs text-[#40493d]">Valor total:</span>
                <span className="text-lg font-bold text-[#2e7d32]">{formatCurrency(k.dailyRate)}</span>
              </div>
            </div>

            <button
              type="button"
              disabled={!selected}
              onClick={() => navigate(`/quiosques/${k.id}/reservar?data=${selected}`)}
              className="w-full py-3.5 px-6 rounded-lg bg-[#2e7d32] hover:bg-[#0d631b] text-white font-bold text-sm flex items-center justify-center gap-2 transition-colors shadow-sm active:scale-[0.99] cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span>Reservar agora</span>
              <Icon name="arrow_forward" className="text-[18px]" />
            </button>
          </div>

          <div className="bg-[#f1f3ff] p-4 rounded-xl border border-[#dce2f7] flex items-center gap-3">
            <Icon name="verified_user" className="text-[#2e7d32] text-[24px]" />
            <div className="flex flex-col">
              <span className="text-xs font-semibold text-[#141b2b]">Data bloqueada por 15 minutos</span>
              <span className="text-xs text-[#40493d]">Ao reservar, o dia fica reservado para você enquanto faz o Pix.</span>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}

function Chip({ icon, text }: { icon: string; text: string }) {
  return (
    <div className="flex items-center gap-1.5 bg-[#f1f3ff] px-2.5 py-1 rounded border border-[#dce2f7]">
      <Icon name={icon} className="text-[#2e7d32] text-[18px]" />
      <span className="font-medium text-[#141b2b]">{text}</span>
    </div>
  );
}
