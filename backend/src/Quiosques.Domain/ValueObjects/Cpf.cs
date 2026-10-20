namespace Quiosques.Domain.ValueObjects;

// RN04: CPF válido pelos dígitos verificadores. Guardado só com os 11 dígitos.
public sealed record Cpf
{
    public const string InvalidMessage = "CPF inválido";

    public string Value { get; }

    private Cpf(string value) => Value = value;

    // RNF07: o CPF aparece mascarado em telas, comprovantes e e-mails (ex.: ***.982.247-**).
    public string Masked => $"***.{Value[3..6]}.{Value[6..9]}-**";

    public static Cpf Parse(string? input) =>
        TryParse(input, out var cpf) ? cpf : throw new DomainException(InvalidMessage);

    public static bool TryParse(string? input, out Cpf cpf)
    {
        cpf = null!;
        if (string.IsNullOrWhiteSpace(input) || input.Any(c => !char.IsAsciiDigit(c) && c is not ('.' or '-' or ' ')))
        {
            return false;
        }

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length != 11 || digits.All(d => d == digits[0]))
        {
            return false;
        }

        if (CheckDigit(digits, 9) != digits[9] - '0' || CheckDigit(digits, 10) != digits[10] - '0')
        {
            return false;
        }

        cpf = new Cpf(digits);
        return true;
    }

    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (length + 1 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    public override string ToString() => Masked;
}
