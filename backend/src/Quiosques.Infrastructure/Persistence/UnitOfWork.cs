using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quiosques.Application.Common;
using Quiosques.Infrastructure.Persistence.Configurations;

namespace Quiosques.Infrastructure.Persistence;

public class UnitOfWork(QuiosquesDbContext context) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        // Cada tentativa começa sem entidades de uma tentativa anterior.
        context.ChangeTracker.Clear();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } postgres)
        {
            throw postgres.ConstraintName switch
            {
                // RN01 / ADR-001
                ReservationConfiguration.ActiveReservationIndex => new DateUnavailableException(),
                ReservationConfiguration.CodeIndex => new DuplicateReservationCodeException(),
                // RN09
                AccountConfiguration.CustomerIndex => new AccountAlreadyExistsException(),
                AccountConfiguration.LoginEmailIndex => new LoginEmailInUseException(),
                _ => exception,
            };
        }
    }
}
