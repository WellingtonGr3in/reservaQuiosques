using Quiosques.Application.Common;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Reservations;

// RF08 / HU04: cancelar a reserva. RN07 vale com ou sem login.
public class CancelReservation(
    IReservationRepository reservations,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const string StatusChangedMessage = "A situação da reserva mudou. Consulte novamente.";

    // Sem login: código + CPF (RN10).
    public async Task<ReservationDetails> ExecuteAsync(string? code, string? cpf, CancellationToken cancellationToken) =>
        await CancelAsync(await ReservationLookup.FindAsync(reservations, code, cpf, cancellationToken), cancellationToken);

    // Com login: só reservas do próprio cliente ("Minhas reservas").
    public async Task<ReservationDetails> ExecuteForCustomerAsync(Guid customerId, string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAndCustomerAsync(code.Trim().ToUpperInvariant(), customerId, cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return await CancelAsync(reservation, cancellationToken);
    }

    // RF15 / RN13: o administrador cancela qualquer reserva ativa, a qualquer momento.
    public async Task<ReservationDetails> ExecuteForAdministratorAsync(string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return await CancelAsync(reservation, cancellationToken, byAdministrator: true);
    }

    private async Task<ReservationDetails> CancelAsync(Reservation reservation, CancellationToken cancellationToken, bool byAdministrator = false)
    {
        var now = clock.UtcNow;
        if (byAdministrator)
        {
            reservation.EnsureCanBeCancelledByAdministrator(now);
        }
        else
        {
            reservation.EnsureCanBeCancelled(now);
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Se o pagamento for confirmado (ou a reserva expirar) ao mesmo tempo, o UPDATE não encontra o status esperado.
            if (!await reservations.TryCancelAsync(reservation.Id, reservation.Status, ct))
            {
                throw new ConflictException(StatusChangedMessage);
            }

            // RN07: reserva confirmada é cancelada com reembolso integral; a pendente, sem cobrança.
            // Um Pix pago depois do cancelamento cai no RN11 (ConfirmPayment).
            if (reservation.Status == ReservationStatus.Confirmed)
            {
                var payment = await payments.GetApprovedByReservationAsync(reservation.Id, ct)
                    ?? throw new InvalidOperationException($"Reserva confirmada {reservation.Code} sem pagamento aprovado.");

                // Reembolso pedido antes do commit: se o gateway falhar, o cancelamento não é gravado.
                // RN14: o pagamento feito na administração é devolvido por ela, fora do sistema.
                if (payment.Method == PaymentMethod.Pix)
                {
                    await gateway.RefundAsync(payment.GatewayTransactionId, ct);
                }

                await payments.MarkRefundedAsync(payment.GatewayTransactionId, ct);
            }

            return true;
        }, cancellationToken);

        var cancelled = (await reservations.GetWithDetailsAsync(reservation.Id, cancellationToken))!;
        return ReservationDetails.From(cancelled, now);
    }
}
