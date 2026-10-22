import type { ReactNode } from 'react';
import { Link, useParams } from 'react-router';
import { useAuth } from '../auth/AuthContext';
import { ErrorBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { StatusBadge } from '../components/StatusBadge';
import { useBookingAccess } from '../lib/bookingAccess';
import { formatCurrency, formatDate, formatTime, weekday } from '../lib/format';
import { useApi } from '../lib/useApi';

// Tela "Comprovante de Reserva" (HU03 / RF07). O mesmo comprovante vai por e-mail.
export function ReceiptPage() {
  const { code = '' } = useParams();
  const { isLoggedIn } = useAuth();
  const access = useBookingAccess(code);
  const receipt = useApi(() => (access ? access.getReceipt() : Promise.resolve(null)), [access]);

  if (!access) {
    return (
      <div className="max-w-[620px] mx-auto px-6 py-8">
        <ErrorBox message="Não encontramos os dados desta reserva neste navegador.">
          <Link to="/consultar" className="underline">
            Consultar a reserva pelo CPF
          </Link>
        </ErrorBox>
      </div>
    );
  }
  if (receipt.loading) return <Loading />;
  if (receipt.error || !receipt.data) {
    return (
      <div className="max-w-[620px] mx-auto px-6 py-8">
        <ErrorBox message={receipt.error?.message ?? 'Reserva não encontrada'} />
      </div>
    );
  }

  const r = receipt.data;
  const confirmed = r.status === 'CONFIRMADA';

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8 flex flex-col items-center">
      <div className="w-full max-w-[620px] bg-[#ffffff] shadow-md rounded-xl border border-[#dce2f7] p-6 sm:p-10 flex flex-col items-center text-center">
        <div className={`w-16 h-16 rounded-full flex items-center justify-center mb-6 ${confirmed ? 'bg-[#acf4a4] text-[#0d631b]' : 'bg-[#e9edff] text-[#40493d]'}`}>
          <Icon name={confirmed ? 'check_circle' : 'info'} className="text-[36px]" filled />
        </div>

        <h1 className="text-3xl font-bold text-[#141b2b] tracking-tight mb-2">
          {confirmed ? 'Reserva confirmada!' : 'Comprovante da reserva'}
        </h1>
        <p className="text-sm text-[#40493d] max-w-md mx-auto mb-8">
          {confirmed
            ? 'O pagamento foi identificado e o seu quiosque está reservado. O comprovante também foi enviado para o seu e-mail.'
            : 'Esta reserva não está confirmada.'}
        </p>

        <div className="w-full bg-[#f1f3ff] rounded-xl p-5 mb-8 flex flex-col items-center border border-[#dce2f7]">
          <span className="text-xs uppercase tracking-wider text-[#40493d] mb-1 font-semibold">Código da reserva</span>
          <div className="flex items-center gap-2">
            <span className="font-mono text-2xl font-bold tracking-widest text-[#0d631b]">{r.code}</span>
            <button
              type="button"
              onClick={() => navigator.clipboard?.writeText(r.code)}
              title="Copiar código"
              className="p-1 text-[#40493d] hover:text-[#0d631b] transition-colors rounded cursor-pointer print:hidden"
            >
              <Icon name="content_copy" className="text-[20px]" />
            </button>
          </div>
        </div>

        <dl className="w-full text-left mb-8 flex flex-col space-y-3.5 text-sm">
          <Row label="Quiosque">
            {r.kioskName} <span className="text-xs text-[#40493d] font-normal">(Capacidade para até {r.kioskCapacity} pessoas)</span>
          </Row>
          <Row label="Data">
            {formatDate(r.date)} ({weekday(r.date)})
          </Row>
          <Row label="Horário">
            {formatTime(r.startTime)} às {formatTime(r.endTime)}
          </Row>
          <Row label="Titular">{r.holderName}</Row>
          <Row label="CPF">
            <span className="font-mono font-normal">{r.maskedCpf}</span>
          </Row>
          <Row label={confirmed ? 'Valor pago' : 'Valor'}>
            {formatCurrency(r.amount)} {confirmed && <span className="text-xs text-[#40493d] font-normal">(Pago via Pix)</span>}
          </Row>
          <Row label="Status">
            <StatusBadge status={r.status} />
          </Row>
        </dl>

        {confirmed && (
          <div className="w-full bg-[#e1e8fd] rounded-lg p-4 mb-8 flex items-start text-left gap-3 border border-[#dce2f7]">
            <Icon name="info" className="text-[#0d631b] text-[22px] shrink-0 mt-0.5" />
            <p className="text-xs text-[#141b2b] leading-relaxed">
              Apresente o código <strong className="font-bold text-[#0d631b]">{r.code}</strong> e um documento com foto na portaria para ter
              acesso às instalações.
            </p>
          </div>
        )}

        <div className="w-full flex flex-col gap-3 print:hidden">
          {confirmed && (
            <button
              type="button"
              onClick={() => window.print()}
              className="w-full h-11 bg-[#0d631b] text-white text-sm font-semibold rounded flex items-center justify-center gap-2 hover:bg-[#2e7d32] transition-colors shadow-sm cursor-pointer"
            >
              <Icon name="print" className="text-[20px]" />
              <span>Imprimir comprovante / Salvar PDF</span>
            </button>
          )}
          <Link
            to={isLoggedIn ? '/minhas-reservas' : '/entrar?aba=criar'}
            className="w-full h-11 bg-[#f1f3ff] text-[#141b2b] text-sm font-semibold rounded flex items-center justify-center hover:bg-[#e9edff] transition-colors border border-[#dce2f7]"
          >
            {isLoggedIn ? 'Ver minhas reservas' : 'Criar conta para acompanhar minhas reservas'}
          </Link>
          <Link to="/" className="inline-flex items-center justify-center text-sm font-semibold text-[#40493d] hover:text-[#0d631b] transition-colors pt-2">
            Voltar ao início
          </Link>
        </div>
      </div>
    </div>
  );
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-baseline justify-between gap-1 py-1 border-b border-[#f1f3ff]">
      <dt className="text-xs text-[#40493d] uppercase tracking-wider w-36 shrink-0 font-semibold">{label}</dt>
      <dd className="font-semibold text-[#141b2b] text-left sm:text-right">{children}</dd>
    </div>
  );
}
