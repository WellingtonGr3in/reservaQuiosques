using Quiosques.Domain.Entities;

namespace Quiosques.Application.Customers;

public interface ICustomerRepository
{
    // Grava o cliente se o CPF ainda não existe e devolve o cadastro do CPF travado até o fim da transação,
    // para que reservas simultâneas do mesmo CPF sejam processadas uma de cada vez (RN08).
    Task<Customer> GetOrCreateForUpdateAsync(Customer candidate, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Customer?> GetByCpfAsync(string cpf, CancellationToken cancellationToken);
}
