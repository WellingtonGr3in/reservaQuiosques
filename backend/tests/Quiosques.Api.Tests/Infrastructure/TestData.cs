using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.Infrastructure;

// Cria registros direto no banco para montar o "Dado que" dos cenários.
public class TestData(PostgresFixture fixture)
{
    private static int _sequence;

    public static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);

    public Task<Customer> CustomerAsync() =>
        AddAsync(new Customer(NextCpf(), "João da Silva", "joao@exemplo.com", "47999998888", Now));

    public Task<Kiosk> KioskAsync(bool isActive = true, string? name = null) =>
        AddAsync(new Kiosk(
            name ?? $"Quiosque Teste {Interlocked.Increment(ref _sequence)}",
            "Churrasqueira coberta",
            30,
            150m,
            "/imagens/quiosques/teste.jpg",
            "Uso das 08:00 às 22:00",
            isActive));

    public async Task<Reservation> ReservationAsync(
        Kiosk kiosk,
        DateOnly date,
        ReservationStatus status,
        DateTimeOffset? expiresAt = null)
    {
        var customer = await CustomerAsync();
        return await AddAsync(new Reservation(
            NextCode(), customer.Id, kiosk.Id, date, kiosk.DailyRate, status, expiresAt ?? Now.AddMinutes(15), Now));
    }

    // Para quiosques criados pela API (só o id é conhecido).
    public async Task<Reservation> ReservationAsync(Guid kioskId, decimal amount, DateOnly date, ReservationStatus status)
    {
        var customer = await CustomerAsync();
        return await AddAsync(new Reservation(NextCode(), customer.Id, kioskId, date, amount, status, Now.AddMinutes(15), Now));
    }

    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        await using var context = fixture.CreateContext();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    // CPF válido e único por chamada (RN04), só com os 11 dígitos.
    public static string NextCpf()
    {
        var digits = (100_000_000 + Interlocked.Increment(ref _sequence)).ToString();
        digits += CheckDigit(digits);
        digits += CheckDigit(digits);
        return digits;
    }

    public static string FormatCpf(string digits) => $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";

    // Código único no formato do DER (QSK- + 6 caracteres), usado só para montar dados de teste.
    public static string NextCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var value = Interlocked.Increment(ref _sequence);
        var chars = new char[6];
        for (var i = 5; i >= 0; i--)
        {
            chars[i] = alphabet[value % alphabet.Length];
            value /= alphabet.Length;
        }

        return "QSK-" + new string(chars);
    }

    private static int CheckDigit(string digits)
    {
        var sum = digits.Select((d, i) => (d - '0') * (digits.Length + 1 - i)).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
