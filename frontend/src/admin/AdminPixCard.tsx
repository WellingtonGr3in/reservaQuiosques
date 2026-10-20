import { QRCodeSVG } from 'qrcode.react';
import { useEffect, useState } from 'react';
import { adminApi, api, ApiError } from '../api/client';
import type { PixPayment } from '../api/types';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { formatCurrency } from '../lib/format';
import { Card, secondaryButton } from './ui';

const POLL_INTERVAL_MS = 5000;
const SHOW_SIMULATE_PAYMENT = import.meta.env.DEV || import.meta.env.VITE_SIMULAR_PAGAMENTO === 'true';

// RF13: Pix da reserva aguardando pagamento, para o administrador mostrar ao cliente no balcão.
// "onChanged" é chamado quando a reserva deixa de estar pendente (paga, expirada etc.).
export function AdminPixCard({ code, onChanged }: { code: string; onChanged: () => void }) {
  const [payment, setPayment] = useState<PixPayment | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [now, setNow] = useState(Date.now());

  useEffect(() => {
    adminApi
      .startPayment(code)
      .then(setPayment)
      .catch((err) => setError(err instanceof ApiError ? err.message : String(err)));
  }, [code]);

  // Atualiza o tempo restante e confere se o Pix já foi confirmado.
  useEffect(() => {
    const tick = setInterval(() => setNow(Date.now()), 1000);
    const poll = setInterval(async () => {
      const current = await adminApi.getReservation(code).catch(() => null);
      if (current && current.reservation.status !== 'PENDENTE_PAGAMENTO') onChanged();
    }, POLL_INTERVAL_MS);
    return () => {
      clearInterval(tick);
      clearInterval(poll);
    };
  }, [code, onChanged]);

  if (error) return <ErrorBox message={error} />;
  if (!payment) return <Loading text="Gerando o Pix..." />;

  const secondsLeft = Math.max(0, Math.floor((new Date(payment.expiresAt).getTime() - now) / 1000));

  return (
    <Card className="relative">
      {SHOW_SIMULATE_PAYMENT && secondsLeft > 0 && (
        <button
          type="button"
          onClick={async () => {
            await api.simulatePayment(payment.transactionId).catch(() => undefined);
            onChanged();
          }}
          title="Simular pagamento do Pix (modo de desenvolvimento)"
          aria-label="Simular pagamento do Pix"
          className="absolute top-2 right-2 w-7 h-7 rounded-full flex items-center justify-center text-[#b07a00] opacity-60 hover:opacity-100 hover:bg-[#fff4d6] transition cursor-pointer"
        >
          <Icon name="bolt" className="text-[16px]" />
        </button>
      )}
      <h2 className="text-base font-bold mb-3">Pagamento via Pix</h2>
      {secondsLeft === 0 ? (
        <p className="text-sm text-[#40493d]">O prazo de 15 minutos para o pagamento terminou.</p>
      ) : (
        <div className="flex flex-col sm:flex-row gap-5 items-center sm:items-start">
          <div className="p-2 bg-[#ffffff] rounded-lg border border-[#dce2f7] shrink-0">
            <QRCodeSVG value={payment.pixCopyPaste} size={168} fgColor="#141b2b" />
          </div>
          <div className="flex flex-col gap-3 w-full min-w-0">
            <div className="text-sm">
              <span className="text-[#40493d]">Valor: </span>
              <strong className="text-[#0d631b]">{formatCurrency(payment.amount)}</strong>
              <span className="text-[#40493d]"> • Tempo restante: </span>
              <strong>
                {String(Math.floor(secondsLeft / 60)).padStart(2, '0')}:{String(secondsLeft % 60).padStart(2, '0')}
              </strong>
            </div>
            <div className="flex gap-2 min-w-0">
              <input readOnly value={payment.pixCopyPaste} className="flex-1 min-w-0 h-10 px-3 rounded bg-[#f1f3ff] border border-[#dce2f7] font-mono text-xs truncate" />
              <button
                type="button"
                onClick={() => {
                  navigator.clipboard?.writeText(payment.pixCopyPaste);
                  setCopied(true);
                  setTimeout(() => setCopied(false), 2000);
                }}
                className={secondaryButton}
              >
                <Icon name={copied ? 'done' : 'content_copy'} className="text-[18px]" />
                {copied ? 'Copiado' : 'Copiar'}
              </button>
            </div>
            <p className="text-xs text-[#707a6c]">Quando o Pix for confirmado, esta tela atualiza sozinha.</p>
          </div>
        </div>
      )}
    </Card>
  );
}
