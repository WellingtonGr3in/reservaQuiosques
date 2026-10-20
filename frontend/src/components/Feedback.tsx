import type { ReactNode } from 'react';
import { Icon } from './Icon';

export function Loading({ text = 'Carregando...' }: { text?: string }) {
  return (
    <div className="flex items-center justify-center gap-2 py-16 text-sm text-[#40493d]">
      <Icon name="sync" className="animate-spin text-[20px]" />
      <span>{text}</span>
    </div>
  );
}

export function InfoBox({ message }: { message: string }) {
  return (
    <div role="status" className="flex items-start gap-2 rounded-lg border border-[#dce2f7] bg-[#f1f3ff] p-3 text-sm text-[#40493d]">
      <Icon name="info" className="mt-0.5 shrink-0 text-[18px] text-[#0d631b]" />
      <span>{message}</span>
    </div>
  );
}

export function ErrorBox({ message, children }: { message: string; children?: ReactNode }) {
  return (
    <div role="alert" className="flex items-start gap-2 rounded-lg border border-[#ffdad6] bg-[#ffdad6]/50 p-3 text-sm text-[#93000a]">
      <Icon name="error" className="mt-0.5 shrink-0 text-[18px]" />
      <div className="flex flex-col gap-1">
        <span>{message}</span>
        {children}
      </div>
    </div>
  );
}
