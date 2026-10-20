import { Icon } from './Icon';

// Etapas do fluxo de reserva (protótipos "Seus Dados" e "Pagamento").
export function Stepper({ steps, current }: { steps: string[]; current: number }) {
  return (
    <div className="mb-8 flex items-center gap-3 flex-wrap print:hidden">
      {steps.map((step, i) => (
        <div key={step} className="flex items-center gap-3">
          {i > 0 && <div className={`h-[2px] w-10 ${i <= current ? 'bg-[#0d631b]' : 'bg-[#dce2f7]'}`}></div>}
          <div className={`flex items-center gap-2 ${i > current ? 'opacity-50' : ''}`}>
            <div
              className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold ${
                i < current ? 'bg-[#acf4a4] text-[#307231]' : i === current ? 'bg-[#2e7d32] text-white' : 'bg-[#dce2f7] text-[#141b2b]'
              }`}
            >
              {i < current ? <Icon name="check" className="text-[14px]" /> : i + 1}
            </div>
            <span className={`text-xs text-[#141b2b] ${i === current ? 'font-bold' : ''}`}>{step}</span>
          </div>
        </div>
      ))}
    </div>
  );
}
