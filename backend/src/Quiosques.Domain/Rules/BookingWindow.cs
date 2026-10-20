namespace Quiosques.Domain.Rules;

// RN03: a reserva deve ser feita com no mínimo 2 e no máximo 90 dias de antecedência.
public static class BookingWindow
{
    public const int MinDaysInAdvance = 2;
    public const int MaxDaysInAdvance = 90;

    public static DateOnly FirstDate(DateOnly today) => today.AddDays(MinDaysInAdvance);

    public static DateOnly LastDate(DateOnly today) => today.AddDays(MaxDaysInAdvance);

    public static bool Contains(DateOnly date, DateOnly today) =>
        date >= FirstDate(today) && date <= LastDate(today);
}
