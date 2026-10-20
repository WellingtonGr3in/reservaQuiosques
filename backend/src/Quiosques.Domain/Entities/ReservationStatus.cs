namespace Quiosques.Domain.Entities;

// Gravado no banco como PENDENTE_PAGAMENTO, CONFIRMADA, EXPIRADA e CANCELADA.
public enum ReservationStatus
{
    PendingPayment,
    Confirmed,
    Expired,
    Cancelled,
}
