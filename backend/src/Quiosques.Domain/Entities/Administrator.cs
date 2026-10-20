namespace Quiosques.Domain.Entities;

// Tabela "administrador" (RF11). Separada das contas de cliente.
public class Administrator
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Administrator() { }

    public Administrator(string name, string email, string passwordHash, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }
}
