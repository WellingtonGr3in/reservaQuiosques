using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Accounts;

// Dados do cliente logado para preencher o formulário de reserva. O CPF vai mascarado (RNF07):
// a reserva logada usa o CPF da conta, então ele não precisa voltar para o navegador.
public record CustomerProfile(string Name, string Email, string Phone, string MaskedCpf);

public class GetCustomerProfile(ICustomerRepository customers)
{
    public async Task<CustomerProfile> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado");
        return new CustomerProfile(customer.Name, customer.Email, customer.Phone, Cpf.Parse(customer.Cpf).Masked);
    }
}
