namespace Quiosques.Domain.Entities;

// Tabela "conta". Opcional; no máximo uma por CPF (RN09).
public class Account
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string LoginEmail { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer Customer { get; private set; } = null!;

    private Account() { }

    public Account(Guid customerId, string loginEmail, string passwordHash, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        CustomerId = customerId;
        LoginEmail = loginEmail;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }
}
