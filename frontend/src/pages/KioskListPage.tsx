import { Link } from 'react-router';
import { api } from '../api/client';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCurrency } from '../lib/format';
import { useApi } from '../lib/useApi';

// Tela "Lista de Quiosques" (HU01 / RF01).
export function KioskListPage() {
  const kiosks = useApi(() => api.listKiosks(), []);

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8">
      <section className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <div className="flex items-center gap-1.5 text-[#0d631b] text-xs font-semibold uppercase tracking-wider mb-1">
            <Icon name="outdoor_grill" className="text-[16px]" />
            <span>Áreas de Lazer Comunitárias</span>
          </div>
          <h1 className="text-3xl font-bold text-[#141b2b] tracking-tight">Quiosques disponíveis</h1>
          <p className="text-sm text-[#40493d] mt-1">
            Selecione um quiosque para ver detalhes e datas disponíveis para reserva com churrasqueira.
          </p>
        </div>
        {kiosks.data && (
          <div className="flex items-center gap-2 bg-[#f1f3ff] px-4 py-2 rounded-lg self-start md:self-auto border border-[#dce2f7]">
            <span className="w-2.5 h-2.5 rounded-full bg-[#2e7d32]"></span>
            <span className="text-xs font-semibold text-[#141b2b]">{kiosks.data.length} quiosques ativos no condomínio</span>
          </div>
        )}
      </section>

      {kiosks.loading && <Loading />}
      {kiosks.error && <ErrorBox message={kiosks.error.message} />}

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {kiosks.data?.map((kiosk) => (
          <article
            key={kiosk.id}
            className="bg-[#ffffff] rounded-xl overflow-hidden border border-[#dce2f7] hover:border-[#2e7d32] hover:shadow-md transition-all duration-200 flex flex-col justify-between group"
          >
            <div>
              <div className="relative w-full h-48 overflow-hidden bg-[#e9edff]">
                <img
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  src={kiosk.photoUrl}
                  alt={kiosk.name}
                />
              </div>

              <div className="p-5 pb-4">
                <h2 className="text-lg text-[#141b2b] font-bold">{kiosk.name}</h2>
                <div className="flex items-center gap-1.5 text-[#40493d] text-xs mt-1">
                  <Icon name="group" className="text-[18px]" />
                  <span>Capacidade: até {kiosk.capacity} pessoas</span>
                </div>

                <div className="mt-4 pt-3 bg-[#f1f3ff] -mx-5 px-5 py-2 flex items-center justify-between border-y border-[#dce2f7]/60">
                  <span className="text-xs text-[#40493d]">Diária completa</span>
                  <span className="text-lg text-[#2e7d32] font-bold">
                    {formatCurrency(kiosk.dailyRate)} <span className="text-xs font-normal text-[#40493d]">/ dia</span>
                  </span>
                </div>

                <ul className="mt-4 space-y-1.5 text-xs text-[#40493d]">
                  {kiosk.items.slice(0, 2).map((item) => (
                    <li key={item} className="flex items-center gap-2">
                      <Icon name="check_circle" className="text-[16px] text-[#0d631b]" />
                      <span>{item}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>

            <div className="p-5 pt-0">
              <Link
                to={`/quiosques/${kiosk.id}`}
                className="w-full bg-[#2e7d32] hover:bg-[#0d631b] text-white font-semibold text-sm py-2.5 px-4 rounded-lg flex items-center justify-center gap-2 transition-colors duration-150 shadow-sm active:scale-[0.99]"
              >
                <span>Ver detalhes e reservar</span>
                <Icon name="arrow_forward" className="text-[18px]" />
              </Link>
            </div>
          </article>
        ))}
      </div>

      <section className="mt-8 bg-[#f1f3ff] rounded-xl p-5 border border-[#dce2f7] flex items-start gap-3">
        <div className="w-10 h-10 rounded-full bg-[#2e7d32]/10 flex items-center justify-center shrink-0 text-[#2e7d32]">
          <Icon name="info" />
        </div>
        <div>
          <h3 className="text-base font-bold text-[#141b2b]">Regulamento geral de uso</h3>
          <p className="text-xs text-[#40493d] mt-0.5 leading-relaxed">
            Horário de utilização: das 08:00 às 22:00. O cancelamento sem cobrança pode ser feito com até 48 horas de antecedência.
          </p>
        </div>
      </section>
    </div>
  );
}
