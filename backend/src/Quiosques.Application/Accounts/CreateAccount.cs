using System.Text;
using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Domain.Entities;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Accounts;

public record CreateAccountCommand(string? Cpf, string? Email, string? Password);

// HU05 / RF09: criar conta vinculada ao CPF. RN09: o CPF precisa já ter feito ao menos uma reserva; o e-mail é só o login.
// As reservas feitas antes com o CPF passam a aparecer no histórico (já estão ligadas ao mesmo cliente).
public class CreateAccount(
    ICustomerRepository customers,
    IAccountRepository accounts,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MinPasswordLength = 8;

    // Limite do bcrypt: só os primeiros 72 bytes da senha são considerados.
    public const int MaxPasswordBytes = 72;

    // CPF inválido e CPF sem reservas dão a mesma resposta.
    public const string NotMatchedMessage = "Não encontramos reservas com este CPF";

    public async Task<AuthenticatedAccount> ExecuteAsync(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var email = LoginEmail.Normalize(command.Email);
        var password = ValidatePassword(command.Password);

        // RN09: o cliente (CPF) é criado na primeira reserva.
        var customer = Cpf.TryParse(command.Cpf, out var cpf)
            ? await customers.GetByCpfAsync(cpf.Value, cancellationToken)
            : null;
        if (customer is null)
        {
            throw new NotFoundException(NotMatchedMessage);
        }

        if (await accounts.ExistsForCustomerAsync(customer.Id, cancellationToken))
        {
            throw new AccountAlreadyExistsException();
        }

        var account = new Account(customer.Id, email, passwordHasher.Hash(password), clock.UtcNow);
        accounts.Add(account);

        // Duas criações simultâneas para o mesmo CPF: o índice ux_conta_cliente_id rejeita a segunda.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticatedAccount(account.Id, account.CustomerId);
    }

    private static string ValidatePassword(string? password)
    {
        if (password is null || password.Length < MinPasswordLength || Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
        {
            throw new ValidationException("A senha deve ter entre 8 e 72 caracteres");
        }

        return password;
    }
}
