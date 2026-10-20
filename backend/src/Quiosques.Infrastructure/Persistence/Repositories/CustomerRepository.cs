using Microsoft.EntityFrameworkCore;
using Quiosques.Application.Customers;
using Quiosques.Domain.Entities;

namespace Quiosques.Infrastructure.Persistence.Repositories;

public class CustomerRepository(QuiosquesDbContext context) : ICustomerRepository
{
    public async Task<Customer> GetOrCreateForUpdateAsync(Customer candidate, CancellationToken cancellationToken)
    {
        // ON CONFLICT evita erro quando duas reservas do mesmo CPF novo chegam ao mesmo tempo.
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO cliente (id, cpf, nome, email, telefone, criado_em)
            VALUES ({candidate.Id}, {candidate.Cpf}, {candidate.Name}, {candidate.Email}, {candidate.Phone}, {candidate.CreatedAt})
            ON CONFLICT (cpf) DO NOTHING
            """,
            cancellationToken);

        // FOR UPDATE trava o cliente até o fim da transação (RN08 sob concorrência).
        var customers = await context.Customers
            .FromSql($"SELECT * FROM cliente WHERE cpf = {candidate.Cpf} FOR UPDATE")
            .ToListAsync(cancellationToken);

        return customers.Single();
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> GetByCpfAsync(string cpf, CancellationToken cancellationToken) =>
        context.Customers.AsNoTracking().SingleOrDefaultAsync(c => c.Cpf == cpf, cancellationToken);
}
