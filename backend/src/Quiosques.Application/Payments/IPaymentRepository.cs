using Quiosques.Domain.Entities;

namespace Quiosques.Application.Payments;

public interface IPaymentRepository
{
    Task<Payment?> GetPendingByReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    Task<Payment?> GetApprovedByReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    void Add(Payment payment);

    // RNF03: muda de PENDENTE para APROVADO só uma vez. Devolve a reserva do pagamento,
    // ou null se o pagamento não existe ou já foi processado.
    Task<Guid?> TryApproveAsync(string transactionId, DateTimeOffset paidAt, CancellationToken cancellationToken);

    Task MarkRefundedAsync(string transactionId, CancellationToken cancellationToken);
}
