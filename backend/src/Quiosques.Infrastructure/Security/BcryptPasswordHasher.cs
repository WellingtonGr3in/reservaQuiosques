using Quiosques.Application.Accounts;

namespace Quiosques.Infrastructure.Security;

// RNF04: bcrypt com custo mínimo 12.
public class BcryptPasswordHasher : IPasswordHasher
{
    public const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
