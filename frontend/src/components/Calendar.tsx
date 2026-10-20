import { useState } from 'react';
import { api } from '../api/client';
import { monthName, parseDate } from '../lib/format';
import { useApi } from '../lib/useApi';
import { ErrorBox } from './Feedback';
import { Icon } from './Icon';

const WEEKDAYS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

// RF03: calendário mensal do quiosque. A situação de cada dia (RN01, RN03, RN05) vem da API;
// a navegação vai do mês atual até o mês do último dia reservável (90 dias, RN03).
export function Calendar({
  kioskId,
  selected,
  onSelect,
}: {
  kioskId: string;
  selected: string | null;
  onSelect: (date: string) => void;
}) {
  const today = new Date();
  const first = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 2);
  const last = new Date(today.getFullYear(), today.getMonth(), today.getDate() + 90);

  const initial = selected ? parseDate(selected) : first;
  const [view, setView] = useState({ year: initial.getFullYear(), month: initial.getMonth() + 1 });

  const calendar = useApi(() => api.getCalendar(kioskId, view.year, view.month), [kioskId, view.year, view.month]);

  const index = (year: number, month: number) => year * 12 + month;
  const canGoBack = index(view.year, view.month) > index(today.getFullYear(), today.getMonth() + 1);
  const canGoForward = index(view.year, view.month) < index(last.getFullYear(), last.getMonth() + 1);

  const move = (delta: number) => {
    const d = new Date(view.year, view.month - 1 + delta, 1);
    setView({ year: d.getFullYear(), month: d.getMonth() + 1 });
  };

  const leadingBlanks = new Date(view.year, view.month - 1, 1).getDay();

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <div>
          <span className="text-xs uppercase tracking-wider text-[#40493d] font-semibold">Selecione o dia</span>
          <h2 className="text-lg font-bold text-[#141b2b]">{monthName(view.year, view.month)}</h2>
        </div>
        <div className="flex items-center gap-1">
          <button
            type="button"
            aria-label="Mês anterior"
            disabled={!canGoBack}
            onClick={() => move(-1)}
            className="w-8 h-8 rounded border border-[#d1d5db] flex items-center justify-center text-[#40493d] hover:bg-[#f1f3ff] transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
          >
            <Icon name="chevron_left" className="text-[18px]" />
          </button>
          <button
            type="button"
            aria-label="Próximo mês"
            disabled={!canGoForward}
            onClick={() => move(1)}
            className="w-8 h-8 rounded border border-[#d1d5db] flex items-center justify-center text-[#40493d] hover:bg-[#f1f3ff] transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
          >
            <Icon name="chevron_right" className="text-[18px]" />
          </button>
        </div>
      </div>

      <div className="w-full">
        <div className="grid grid-cols-7 gap-1 text-center mb-2">
          {WEEKDAYS.map((day) => (
            <span key={day} className="text-xs font-semibold text-[#40493d] py-1">
              {day}
            </span>
          ))}
        </div>

        {calendar.error ? (
          <ErrorBox message={calendar.error.message} />
        ) : (
          <div className={`grid grid-cols-7 gap-1.5 text-center font-mono text-sm select-none ${calendar.loading ? 'opacity-50' : ''}`}>
            {Array.from({ length: leadingBlanks }, (_, i) => (
              <span key={`blank-${i}`} />
            ))}
            {calendar.data?.days.map(({ date, status }) => {
              const day = parseDate(date).getDate();
              if (date === selected) {
                return (
                  <button
                    key={date}
                    type="button"
                    aria-pressed="true"
                    className="h-10 rounded flex items-center justify-center bg-[#2e7d32] text-white font-bold shadow-md shadow-[#2e7d32]/20"
                  >
                    {day}
                  </button>
                );
              }
              if (status === 'LIVRE') {
                return (
                  <button
                    key={date}
                    type="button"
                    onClick={() => onSelect(date)}
                    className="h-10 rounded flex items-center justify-center bg-[#f1f3ff] text-[#141b2b] hover:bg-[#2e7d32] hover:text-white transition-colors cursor-pointer"
                  >
                    {day}
                  </button>
                );
              }
              return (
                <button
                  key={date}
                  type="button"
                  disabled
                  title={status === 'OCUPADO' ? 'Ocupado' : 'Indisponível'}
                  className={`h-10 rounded flex items-center justify-center cursor-not-allowed ${
                    status === 'OCUPADO' ? 'bg-[#e1e8fd]/60 text-[#40493d]/40 line-through' : 'text-[#40493d]/30'
                  }`}
                >
                  {day}
                </button>
              );
            })}
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-2 pt-2 text-[#40493d] text-xs">
        <Legend className="bg-[#2e7d32]" label="Selecionado" />
        <Legend className="bg-[#dce2f7]" label="Ocupado" />
        <Legend className="bg-[#f1f3ff] border border-[#dce2f7]" label="Disponível" />
        <Legend className="border border-dashed border-[#dce2f7]" label="Indisponível" />
      </div>
      <p className="text-xs text-[#707a6c]">
        Reservas com no mínimo 2 e no máximo 90 dias de antecedência (de {first.toLocaleDateString('pt-BR')} a{' '}
        {last.toLocaleDateString('pt-BR')}).
      </p>
    </div>
  );
}

function Legend({ className, label }: { className: string; label: string }) {
  return (
    <div className="flex items-center gap-1.5">
      <span className={`w-3.5 h-3.5 rounded inline-block ${className}`}></span>
      <span>{label}</span>
    </div>
  );
}
