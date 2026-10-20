namespace Quiosques.Domain.Entities;

// Tabela "cliente". Identificado pelo CPF (ADR-004).
public class Customer
{
    public Guid Id { get; private set; }
    public string Cpf { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public Account? Account { get; private set; }
    public ICollection<Reservation> Reservations { get; private set; } = [];

    private Customer() { }

    public Customer(string cpf, string name, string email, string phone, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Cpf = cpf;
        Name = name;
        Email = email;
        Phone = phone;
        CreatedAt = createdAt;
    }

    // RN12: nova reserva com dados diferentes atualiza o cadastro com os mais recentes.
    public void UpdateContact(string name, string email, string phone)
    {
        Name = name;
        Email = email;
        Phone = phone;
    }
}
