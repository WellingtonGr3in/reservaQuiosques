using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Accounts;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class AccountRepository(QuiosquesDbContext context) : IAccountRepository
{
    public Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        context.Accounts.AnyAsync(a => a.CustomerId == customerId, cancellationToken);

    public Task<Account?> GetByLoginEmailAsync(string loginEmail, CancellationToken cancellationToken) =>
        context.Accounts.AsNoTracking().SingleOrDefaultAsync(a => a.LoginEmail == loginEmail, cancellationToken);

    public void Add(Account account) => context.Accounts.Add(account);
}
