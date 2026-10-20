using Quiosques.Application.Common;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Payments;

public record PixPayment(string ReservationCode, decimal Amount, string PixCopyPaste, DateTimeOffset ExpiresAt, string TransactionId);

// RF05: gera a cobrança Pix (QR Code e copia-e-cola) da reserva aguardando pagamento.
public class StartPayment(
    IReservationRepository reservations,
    IPaymentRepository payments,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    // Sem login: código + CPF (RN10).
    public async Task<PixPayment> ExecuteAsync(string? code, string? cpf, CancellationToken cancellationToken) =>
        await ChargeAsync(await ReservationLookup.FindAsync(reservations, code, cpf, cancellationToken), cancellationToken);

    // Com login: só reservas do próprio cliente.
    public async Task<PixPayment> ExecuteForCustomerAsync(Guid customerId, string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAndCustomerAsync(code.Trim().ToUpperInvariant(), customerId, cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return await ChargeAsync(reservation, cancellationToken);
    }

    // Administrador (RF13): mostra o Pix de uma reserva aguardando pagamento para o cliente pagar no balcão.
    public async Task<PixPayment> ExecuteForAdministratorAsync(string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return await ChargeAsync(reservation, cancellationToken);
    }

    private async Task<PixPayment> ChargeAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        reservation.EnsureAwaitingPayment(clock.UtcNow);

        // Abrir a tela de pagamento de novo reaproveita a cobrança já gerada.
        var pending = await payments.GetPendingByReservationAsync(reservation.Id, cancellationToken);
        PixCharge charge;
        if (pending is not null)
        {
            charge = await gateway.GetPixChargeAsync(pending.GatewayTransactionId, cancellationToken);
        }
        else
        {
            charge = await gateway.CreatePixChargeAsync(reservation.Code, reservation.Amount, reservation.ExpiresAt, cancellationToken);
            payments.Add(new Payment(reservation.Id, charge.TransactionId, reservation.Amount, PaymentStatus.Pending, paidAt: null));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new PixPayment(reservation.Code, reservation.Amount, charge.CopyPaste, reservation.ExpiresAt, charge.TransactionId);
    }
}
