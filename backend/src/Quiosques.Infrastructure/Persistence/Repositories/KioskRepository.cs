using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Kiosks;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class KioskRepository(QuiosquesDbContext context) : IKioskRepository
{
    public async Task<IReadOnlyList<Kiosk>> ListActiveAsync(CancellationToken cancellationToken) =>
        await context.Kiosks
            .AsNoTracking()
            .Include(k => k.Items)
            .Where(k => k.IsActive)
            .OrderBy(k => k.Name)
            .ToListAsync(cancellationToken);

    public Task<Kiosk?> GetActiveAsync(Guid id, CancellationToken cancellationToken) =>
        context.Kiosks
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.Id == id && k.IsActive, cancellationToken);

    public Task<Kiosk?> GetActiveWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Kiosks
            .AsNoTracking()
            .Include(k => k.Photos)
            .Include(k => k.Items)
            .AsSplitQuery()
            .SingleOrDefaultAsync(k => k.Id == id && k.IsActive, cancellationToken);

    public Task<bool> IsActiveAsync(Guid id, CancellationToken cancellationToken) =>
        context.Kiosks.AnyAsync(k => k.Id == id && k.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Kiosk>> ListAllWithDetailsAsync(CancellationToken cancellationToken) =>
        await WithDetails().AsNoTracking().OrderBy(k => k.Name).ToListAsync(cancellationToken);

    public Task<Kiosk?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        WithDetails().AsNoTracking().SingleOrDefaultAsync(k => k.Id == id, cancellationToken);

    public Task<Kiosk?> GetWithDetailsForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        WithDetails().SingleOrDefaultAsync(k => k.Id == id, cancellationToken);

    public void Add(Kiosk kiosk) => context.Kiosks.Add(kiosk);

    // Fotos e itens são apagados junto (ON DELETE CASCADE).
    public void Remove(Kiosk kiosk) => context.Kiosks.Remove(kiosk);

    private IQueryable<Kiosk> WithDetails() =>
        context.Kiosks.Include(k => k.Photos).Include(k => k.Items).AsSplitQuery();
}
