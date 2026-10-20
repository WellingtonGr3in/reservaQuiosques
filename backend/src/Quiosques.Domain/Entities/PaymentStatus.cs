namespace Quiosques.Domain.Entities;

// Gravado no banco como PENDENTE, APROVADO e REEMBOLSADO.
public enum PaymentStatus
{
    Pending,
    Approved,
    Refunded,
}
