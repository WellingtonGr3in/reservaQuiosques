import type { ReactNode } from 'react';

// Peças visuais comuns da área de administração (mesmo visual do resto do sistema).

export const adminInput =
  'w-full h-10 px-3 bg-[#ffffff] text-[#141b2b] text-sm border border-[#d1d5db] rounded focus:border-[#2e7d32] focus:ring-1 focus:ring-[#2e7d32] outline-none transition-colors';

export const primaryButton =
  'inline-flex items-center justify-center gap-2 bg-[#2e7d32] hover:bg-[#0d631b] text-white text-sm font-semibold h-10 px-4 rounded-lg shadow-sm transition-colors cursor-pointer disabled:opacity-60';

export const secondaryButton =
  'inline-flex items-center justify-center gap-2 bg-[#ffffff] hover:bg-[#e9edff] text-[#141b2b] text-sm font-semibold h-10 px-4 rounded-lg border border-[#d1d5db] transition-colors cursor-pointer disabled:opacity-60';

export const dangerButton =
  'inline-flex items-center justify-center gap-2 bg-[#ba1a1a] hover:bg-[#93000a] text-white text-sm font-semibold h-10 px-4 rounded-lg shadow-sm transition-colors cursor-pointer disabled:opacity-60';

export function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-3 mb-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
        {subtitle && <p className="text-sm text-[#40493d] mt-1">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

export function Field({ label, htmlFor, hint, children }: { label: string; htmlFor: string; hint?: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={htmlFor} className="text-xs font-semibold text-[#40493d]">
        {label}
      </label>
      {children}
      {hint && <span className="text-xs text-[#707a6c]">{hint}</span>}
    </div>
  );
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <section className={`bg-[#ffffff] rounded-xl border border-[#dce2f7] shadow-sm p-5 ${className}`}>{children}</section>;
}

export const PAID_WITH_LABEL: Record<string, string> = {
  PIX: 'Pix',
  ADMINISTRACAO: 'Na administração',
};
