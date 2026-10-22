import { QRCodeSVG } from 'qrcode.react';
import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { api, ApiError } from '../api/client';
import type { PixPayment, Receipt } from '../api/types';
import { ErrorBox, InfoBox, Loading } from '../components/Feedback';
import { Icon } from '../components/Icon';
import { Stepper } from '../components/Stepper';
import { useBookingAccess } from '../lib/bookingAccess';
import { formatCurrency, formatDate } from '../lib/format';

const POLL_INTERVAL_MS = 5000;

// O gateway é simulado (FakePaymentGateway). O botão de simular aparece no "npm run dev" e no build
// do Docker para a apresentação (VITE_SIMULAR_PAGAMENTO=true no docker-compose.yml).
const SHOW_SIMULATE_PAYMENT = import.meta.env.DEV || import.meta.env.VITE_SIMULAR_PAGAMENTO === 'true';

// Tela "Pagamento" (HU03 / RF05, RF06): cobrança Pix com QR Code, copia-e-cola e tempo restante (RN05).
export function PaymentPage() {
  const { code = '' } = useParams();
  const navigate = useNavigate();
  const access = useBookingAccess(code);

  const [payment, setPayment] = useState<PixPayment | null>(null);
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<{ text: string; isError: boolean } | null>(null);
  const [checking, setChecking] = useState(false);
  const [copied, setCopied] = useState(false);
  const [now, setNow] = useState(Date.now());

  // RF06: a confirmação vem do gateway; aqui só se consulta a situação da reserva.
  const checkStatus = useCallback(async () => {
    if (!access) return null;
    const current = await access.getReceipt();
    setReceipt(current);
    if (current.status === 'CONFIRMADA') navigate(`/reservas/${code}/comprovante`, { replace: true });
    return current;
  }, [code, access, navigate]);

  useEffect(() => {
    if (!access) return;
    access
      .startPayment()
      .then(setPayment)
      .catch(async (err) => {
        // A reserva pode já estar paga (ex.: voltou para esta tela) ou expirada.
        const current = await checkStatus().catch(() => null);
        if (current?.status !== 'CONFIRMADA') setError(err instanceof ApiError ? err.message : String(err));
      });
    checkStatus().catch(() => undefined);
  }, [access, checkStatus]);

  useEffect(() => {
    const tick = setInterval(() => setNow(Date.now()), 1000);
    const poll = setInterval(() => checkStatus().catch(() => undefined), POLL_INTERVAL_MS);
    return () => {
      clearInterval(tick);
      clearInterval(poll);
    };
  }, [checkStatus]);

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
  if (error) {
    return (
      <div className="max-w-[620px] mx-auto px-6 py-8">
        <ErrorBox message={error}>
          <Link to="/" className="underline">
            Escolher outro quiosque ou data
          </Link>
        </ErrorBox>
      </div>
    );
  }
  if (!payment) return <Loading text="Gerando a cobrança Pix..." />;

  const secondsLeft = Math.max(0, Math.floor((new Date(payment.expiresAt).getTime() - now) / 1000));
  const expired = secondsLeft === 0;

  const copy = () => {
    navigator.clipboard?.writeText(payment.pixCopyPaste);
    setCopied(true);
    setTimeout(() => setCopied(false), 2500);
  };

  const confirmPaid = async () => {
    setChecking(true);
    setNotice(null);
    try {
      const current = await checkStatus();
      if (current && current.status !== 'CONFIRMADA') {
        setNotice({ text: 'Ainda não identificamos o pagamento. Assim que o Pix for compensado, esta tela avança sozinha.', isError: false });
      }
    } catch (err) {
      setNotice({ text: err instanceof ApiError ? err.message : String(err), isError: true });
    } finally {
      setChecking(false);
    }
  };

  // Só em desenvolvimento: o gateway é simulado (FakePaymentGateway), então ninguém consegue pagar o Pix de verdade.
  const simulatePayment = async () => {
    setChecking(true);
    try {
      await api.simulatePayment(payment.transactionId);
      await checkStatus();
    } catch (err) {
      setNotice({ text: err instanceof ApiError ? err.message : String(err), isError: true });
    } finally {
      setChecking(false);
    }
  };

  return (
    <div className="max-w-[1100px] mx-auto px-6 py-8 flex flex-col items-center">
      <div className="w-full max-w-[540px] flex flex-col gap-6">
        <Stepper steps={['Identificação', 'Pagamento Pix', 'Confirmação']} current={1} />

        <div className="relative bg-[#ffffff] rounded-xl shadow-sm border border-[#dce2f7] p-6 sm:p-8 flex flex-col gap-6">
          {/* Atalho discreto para a apresentação: aprova o Pix no gateway simulado. */}
          {SHOW_SIMULATE_PAYMENT && !expired && (
            <button
              type="button"
              disabled={checking}
              onClick={simulatePayment}
              title="Simular pagamento do Pix (modo de desenvolvimento)"
              aria-label="Simular pagamento do Pix"
              className="absolute top-2 right-2 w-7 h-7 rounded-full flex items-center justify-center text-[#b07a00] opacity-60 hover:opacity-100 hover:bg-[#fff4d6] transition cursor-pointer"
            >
              <Icon name="bolt" className="text-[16px]" />
            </button>
          )}
          <div className="flex flex-col gap-1 text-center">
            <h1 className="text-2xl font-bold text-[#141b2b] tracking-tight">Pagamento via Pix</h1>
            <p className="text-sm text-[#40493d] max-w-sm mx-auto">Escaneie o QR Code ou copie o código Pix para concluir sua reserva.</p>
          </div>

          <div className="bg-[#f1f3ff] border border-[#dce2f7] rounded-lg p-4 flex items-center justify-between">
            <div className="flex flex-col">
              <span className="text-xs text-[#40493d] font-medium">Total a transferir</span>
              <span className="text-2xl font-bold text-[#0d631b] tracking-tight">{formatCurrency(payment.amount)}</span>
            </div>
            {receipt && (
              <div className="flex flex-col items-end text-right">
                <span className="text-xs text-[#40493d]">Reserva {payment.reservationCode}</span>
                <span className="text-sm font-semibold text-[#141b2b]">{receipt.kioskName}</span>
                <span className="text-xs text-[#40493d]">{formatDate(receipt.date)} • Dia todo</span>
              </div>
            )}
          </div>

          {expired ? (
            <ErrorBox message="O prazo de 15 minutos para o pagamento terminou e a data foi liberada.">
              <Link to="/" className="underline">
                Fazer uma nova reserva
              </Link>
            </ErrorBox>
          ) : (
            <>
              <div className="flex flex-col items-center justify-center gap-3">
                <div className="p-3 bg-[#ffffff] rounded-xl shadow-md border border-[#dce2f7]">
                  <QRCodeSVG value={payment.pixCopyPaste} size={200} fgColor="#141b2b" />
                </div>
                <div className="flex items-center gap-1.5 px-3 py-1 bg-[#f1f3ff] border border-[#dce2f7] rounded-full">
                  <Icon name="schedule" className="text-[16px] text-[#40493d]" />
                  <span className="text-xs text-[#40493d]">Tempo restante:</span>
                  <span className="text-xs font-bold text-[#0d631b] tracking-wide">
                    {String(Math.floor(secondsLeft / 60)).padStart(2, '0')}:{String(secondsLeft % 60).padStart(2, '0')}
                  </span>
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <span className="text-xs font-semibold text-[#40493d]">Código Pix Copia e Cola</span>
                <div className="relative flex items-center">
                  <input
                    readOnly
                    onClick={copy}
                    value={payment.pixCopyPaste}
                    className="w-full h-11 pl-3 pr-28 rounded-lg bg-[#f1f3ff] border border-[#dce2f7] font-mono text-xs text-[#141b2b] outline-none truncate cursor-pointer hover:bg-[#e9edff] transition-colors"
                  />
                  <button
                    type="button"
                    onClick={copy}
                    className={`absolute right-1 px-3 py-1.5 h-9 text-white text-xs font-semibold rounded flex items-center gap-1.5 transition-colors cursor-pointer shadow-sm ${
                      copied ? 'bg-[#2a6b2c]' : 'bg-[#0d631b] hover:bg-[#2e7d32]'
                    }`}
                  >
                    <Icon name={copied ? 'done' : 'content_copy'} className="text-[16px]" />
                    <span>{copied ? 'Copiado!' : 'Copiar'}</span>
                  </button>
                </div>
              </div>

              <div className="bg-[#f1f3ff]/60 border border-[#dce2f7] rounded-xl p-4 flex flex-col gap-2.5 text-xs text-[#40493d]">
                <h2 className="text-sm font-bold text-[#141b2b]">Como pagar:</h2>
                <Step n={1} text="Abra o aplicativo do seu banco." />
                <Step n={2} text="Escolha pagar com Pix, por QR Code ou copia-e-cola." />
                <Step n={3} text={`Confirme o valor de ${formatCurrency(payment.amount)} e conclua a transação.`} />
              </div>

              {notice && (notice.isError ? <ErrorBox message={notice.text} /> : <InfoBox message={notice.text} />)}

              <div className="flex flex-col gap-3 pt-2">
                <button
                  type="button"
                  disabled={checking}
                  onClick={confirmPaid}
                  className="w-full h-12 bg-[#0d631b] hover:bg-[#1b5e20] text-white font-bold text-sm rounded-lg shadow flex items-center justify-center gap-2 transition-all cursor-pointer disabled:opacity-80"
                >
                  <Icon name={checking ? 'sync' : 'check_circle'} className={`text-[20px] ${checking ? 'animate-spin' : ''}`} />
                  <span>{checking ? 'Verificando pagamento...' : 'Já paguei'}</span>
                </button>

              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function Step({ n, text }: { n: number; text: string }) {
  return (
    <div className="flex items-start gap-2.5">
      <span className="w-5 h-5 rounded-full bg-[#dce2f7] text-[#141b2b] font-bold flex items-center justify-center shrink-0">{n}</span>
      <p>{text}</p>
    </div>
  );
}
