using Quiosques.Application.Common;

namespace Quiosques.Application.Accounts;

// HU05 / RF09: login com e-mail e senha.
public class Login(IAccountRepository accounts, IPasswordHasher passwordHasher)
{
    public const string InvalidCredentialsMessage = "E-mail ou senha inválidos";

    // Hash de uma senha qualquer, conferido quando o e-mail não existe: a resposta leva o mesmo tempo
    // e não revela quais e-mails têm conta. Gerado uma vez; uma corrida aqui só gera o hash duas vezes.
    private static string? _dummyHash;

    public async Task<AuthenticatedAccount> ExecuteAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        var account = string.IsNullOrWhiteSpace(email)
            ? null
            : await accounts.GetByLoginEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);

        var hash = account?.PasswordHash ?? (_dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString()));
        var valid = passwordHasher.Verify(password ?? "", hash);
        if (account is null || !valid)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        return new AuthenticatedAccount(account.Id, account.CustomerId);
    }
}
