using Quiosques.Application.Common;
using Quiosques.Application.Notifications;
using Quiosques.Application.Reservations;

namespace Quiosques.Application.Payments;

public enum PaymentOutcome
{
    // Pagamento ainda não aprovado no gateway.
    NotApproved,

    // Notificação repetida ou de transação desconhecida (RNF03).
    AlreadyProcessed,

    // RN06: reserva confirmada e comprovante enviado.
    Confirmed,

    // RN11: pagamento aprovado depois que a reserva deixou de estar pendente; valor reembolsado.
    Refunded,
}

// RF06: confirma a reserva quando o gateway avisa que o Pix foi aprovado.
public class ConfirmPayment(
    IPaymentGateway gateway,
    IPaymentRepository payments,
    IReservationRepository reservations,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<PaymentOutcome> ExecuteAsync(string transactionId, CancellationToken cancellationToken)
    {
        if (await gateway.GetStatusAsync(transactionId, cancellationToken) != GatewayPaymentStatus.Approved)
        {
            return PaymentOutcome.NotApproved;
        }

        var (outcome, reservationId) = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var reservationId = await payments.TryApproveAsync(transactionId, clock.UtcNow, ct);
            if (reservationId is null)
            {
                return (PaymentOutcome.AlreadyProcessed, Guid.Empty);
            }

            if (await reservations.TryConfirmAsync(reservationId.Value, ct))
            {
                return (PaymentOutcome.Confirmed, reservationId.Value);
            }

            // RN11: a reserva expirou (ou foi cancelada) antes da aprovação. O reembolso é pedido
            // antes do commit: se o gateway falhar, nada é gravado e a notificação pode ser reprocessada.
            await gateway.RefundAsync(transactionId, ct);
            await payments.MarkRefundedAsync(transactionId, ct);
            return (PaymentOutcome.Refunded, reservationId.Value);
        }, cancellationToken);

        if (outcome == PaymentOutcome.Confirmed)
        {
            // RF07: comprovante por e-mail, só depois de a confirmação estar gravada.
            var reservation = (await reservations.GetWithDetailsAsync(reservationId, cancellationToken))!;
            await emailSender.SendReservationReceiptAsync(
                reservation.Customer.Email, ReservationReceipt.From(reservation), cancellationToken);
        }

        return outcome;
    }
}
