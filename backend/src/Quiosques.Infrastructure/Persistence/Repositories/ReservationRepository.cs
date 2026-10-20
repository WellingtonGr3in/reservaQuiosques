using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Admin;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class ReservationRepository(QuiosquesDbContext context) : IReservationRepository
{
    public async Task<IReadOnlySet<DateOnly>> GetActiveReservationDatesAsync(
        Guid kioskId,
        DateOnly from,
        DateOnly to,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var dates = await context.Reservations
            .AsNoTracking()
            .Where(r => r.KioskId == kioskId && r.ReservationDate >= from && r.ReservationDate <= to)
            .Where(Reservation.IsActiveAt(now))
            .Select(r => r.ReservationDate)
            .ToListAsync(cancellationToken);

        return dates.ToHashSet();
    }

    public Task<int> CountActiveByCustomerAsync(Guid customerId, DateOnly today, DateTimeOffset now, CancellationToken cancellationToken) =>
        context.Reservations
            .Where(r => r.CustomerId == customerId && r.ReservationDate >= today)
            .Where(Reservation.IsActiveAt(now))
            .CountAsync(cancellationToken);

    public Task ExpireOverduePendingAsync(Guid kioskId, DateOnly date, DateTimeOffset now, CancellationToken cancellationToken) =>
        context.Reservations
            .Where(r => r.KioskId == kioskId
                && r.ReservationDate == date
                && r.Status == ReservationStatus.PendingPayment
                && r.ExpiresAt <= now)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, ReservationStatus.Expired), cancellationToken);

    public void Add(Reservation reservation) => context.Reservations.Add(reservation);

    public Task<Reservation?> GetByCodeAndCpfAsync(string code, string cpf, CancellationToken cancellationToken) =>
        WithDetails().SingleOrDefaultAsync(r => r.Code == code && r.Customer.Cpf == cpf, cancellationToken);

    public Task<Reservation?> GetByCodeAndCustomerAsync(string code, Guid customerId, CancellationToken cancellationToken) =>
        WithDetails().SingleOrDefaultAsync(r => r.Code == code && r.CustomerId == customerId, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await WithDetails()
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.ReservationDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Reservation?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        WithDetails().Include(r => r.Payments).SingleOrDefaultAsync(r => r.Code == code, cancellationToken);

    public Task<Reservation?> GetByCodeForUpdateAsync(string code, CancellationToken cancellationToken) =>
        context.Reservations.SingleOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> SearchAsync(ReservationSearch search, CancellationToken cancellationToken)
    {
        var query = WithDetails().Include(r => r.Payments).AsSplitQuery();
        if (search.From is { } from) query = query.Where(r => r.ReservationDate >= from);
        if (search.To is { } to) query = query.Where(r => r.ReservationDate <= to);
        if (search.KioskId is { } kioskId) query = query.Where(r => r.KioskId == kioskId);
        if (search.Status is { } status) query = query.Where(r => r.Status == status);
        if (search.Cpf is { } cpf) query = query.Where(r => r.Customer.Cpf == cpf);

        return await query
            .OrderByDescending(r => r.ReservationDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyForKioskAsync(Guid kioskId, CancellationToken cancellationToken) =>
        context.Reservations.AnyAsync(r => r.KioskId == kioskId, cancellationToken);

    public Task<Reservation?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        WithDetails().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<bool> TryConfirmAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Reservations
            .Where(r => r.Id == id && r.Status == ReservationStatus.PendingPayment)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, ReservationStatus.Confirmed), cancellationToken) == 1;

    public async Task<bool> TryCancelAsync(Guid id, ReservationStatus expectedStatus, CancellationToken cancellationToken) =>
        await context.Reservations
            .Where(r => r.Id == id && r.Status == expectedStatus)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, ReservationStatus.Cancelled), cancellationToken) == 1;

    public Task<int> ExpireAllOverduePendingAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        context.Reservations
            .Where(r => r.Status == ReservationStatus.PendingPayment && r.ExpiresAt <= now)
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Status, ReservationStatus.Expired), cancellationToken);

    private IQueryable<Reservation> WithDetails() =>
        context.Reservations
            .AsNoTracking()
            .Include(r => r.Kiosk)
            .Include(r => r.Customer);
}
