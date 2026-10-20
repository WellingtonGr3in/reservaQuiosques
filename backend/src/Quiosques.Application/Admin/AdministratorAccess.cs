using Quiosques.Application.Accounts;
using Quiosques.Application.Common;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Admin;

public interface IAdministratorRepository
{
    Task<Administrator?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    void Add(Administrator administrator);
}

// Administrador autenticado; a Api transforma em token JWT com o papel "admin".
public record AuthenticatedAdministrator(Guid Id, string Name);

// RF11: login do administrador com e-mail e senha.
public class AdministratorLogin(IAdministratorRepository administrators, IPasswordHasher passwordHasher)
{
    // Hash conferido quando o e-mail não existe, para a resposta levar o mesmo tempo.
    private static string? _dummyHash;

    public async Task<AuthenticatedAdministrator> ExecuteAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        var administrator = string.IsNullOrWhiteSpace(email)
            ? null
            : await administrators.GetByEmailAsync(email.Trim().ToLowerInvariant(), cancellationToken);

        var hash = administrator?.PasswordHash ?? (_dummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString()));
        if (!passwordHasher.Verify(password ?? "", hash) || administrator is null)
        {
            throw new UnauthorizedException(Login.InvalidCredentialsMessage);
        }

        return new AuthenticatedAdministrator(administrator.Id, administrator.Name);
    }
}

// Cria o primeiro administrador quando a API sobe, com os dados da configuração (docs/05-modelagem-dados.md).
public class EnsureDefaultAdministrator(
    IAdministratorRepository administrators,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    // O login do administrador pode ser um e-mail ou um usuário simples (ex.: "admin").
    public async Task ExecuteAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length is 0 or > 254)
        {
            throw new ValidationException("Login do administrador inválido");
        }

        if (await administrators.GetByEmailAsync(normalized, cancellationToken) is not null)
        {
            return;
        }

        administrators.Add(new Administrator(name, normalized, passwordHasher.Hash(password), clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
