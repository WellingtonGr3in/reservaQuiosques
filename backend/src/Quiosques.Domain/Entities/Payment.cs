namespace Quiosques.Domain.Entities;

// Tabela "pagamento". Uma reserva pode ter várias tentativas de pagamento.
public class Payment
{
    public Guid Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public string GatewayTransactionId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    private Payment() { }

    public Payment(
        Guid reservationId,
        string gatewayTransactionId,
        decimal amount,
        PaymentStatus status,
        DateTimeOffset? paidAt,
        PaymentMethod method = PaymentMethod.Pix)
    {
        Id = Guid.CreateVersion7();
        ReservationId = reservationId;
        GatewayTransactionId = gatewayTransactionId;
        Amount = amount;
        Status = status;
        PaidAt = paidAt;
        Method = method;
    }

    // RN14: pagamento feito na administração. O identificador interno ocupa o lugar do id do gateway.
    public static Payment RegisteredByAdministration(Reservation reservation, DateTimeOffset paidAt) =>
        new(reservation.Id, $"ADM-{reservation.Code}", reservation.Amount, PaymentStatus.Approved, paidAt, PaymentMethod.Administration);
}
