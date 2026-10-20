using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class PaymentRepository(QuiosquesDbContext context) : IPaymentRepository
{
    public Task<Payment?> GetPendingByReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        context.Payments
            .AsNoTracking()
            .Where(p => p.ReservationId == reservationId && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Payment?> GetApprovedByReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        context.Payments
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.ReservationId == reservationId && p.Status == PaymentStatus.Approved, cancellationToken);

    public void Add(Payment payment) => context.Payments.Add(payment);

    public async Task<Guid?> TryApproveAsync(string transactionId, DateTimeOffset paidAt, CancellationToken cancellationToken)
    {
        // UPDATE condicional: com duas notificações simultâneas, a segunda espera a trava da linha
        // e não encontra mais o status PENDENTE (RNF03).
        var updated = await context.Payments
            .Where(p => p.GatewayTransactionId == transactionId && p.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(set => set
                .SetProperty(p => p.Status, PaymentStatus.Approved)
                .SetProperty(p => p.PaidAt, paidAt), cancellationToken);

        if (updated == 0)
        {
            return null;
        }

        return await context.Payments
            .Where(p => p.GatewayTransactionId == transactionId)
            .Select(p => p.ReservationId)
            .SingleAsync(cancellationToken);
    }

    public Task MarkRefundedAsync(string transactionId, CancellationToken cancellationToken) =>
        context.Payments
            .Where(p => p.GatewayTransactionId == transactionId)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.Status, PaymentStatus.Refunded), cancellationToken);
}
