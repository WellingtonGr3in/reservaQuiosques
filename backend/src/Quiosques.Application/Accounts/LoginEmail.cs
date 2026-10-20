using System.Text.RegularExpressions;
using Quiosques.Application.Common;

namespace Quiosques.Application.Accounts;

// E-mail de login: comparado sem diferenciar maiúsculas e minúsculas.
public static partial class LoginEmail
{
    public static string Normalize(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? "";
        if (normalized.Length > 254 || !Pattern().IsMatch(normalized))
        {
            throw new ValidationException("E-mail inválido");
        }

        return normalized;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Pattern();
}
