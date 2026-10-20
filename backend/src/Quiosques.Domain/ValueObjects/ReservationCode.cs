using System.Security.Cryptography;

namespace Quiosques.Domain.ValueObjects;

// Código da reserva (docs/05-modelagem-dados.md): "QSK-" + 6 caracteres sem O, I, 0 e 1.
public static class ReservationCode
{
    public const string Prefix = "QSK-";
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int RandomLength = 6;

    public static string Generate() =>
        Prefix + new string(RandomNumberGenerator.GetItems<char>(Alphabet, RandomLength));
}
