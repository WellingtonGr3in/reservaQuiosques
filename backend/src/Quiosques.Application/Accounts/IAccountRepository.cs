using Quiosques.Domain.Entities;

namespace Quiosques.Application.Accounts;

public interface IAccountRepository
{
    Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    Task<Account?> GetByLoginEmailAsync(string loginEmail, CancellationToken cancellationToken);

    void Add(Account account);
}

// RNF04: senhas guardadas com bcrypt, custo mínimo 12.
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

// Conta autenticada; a Api transforma em token JWT.
public record AuthenticatedAccount(Guid AccountId, Guid CustomerId);
