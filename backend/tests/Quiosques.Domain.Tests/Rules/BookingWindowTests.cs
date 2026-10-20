using Quiosques.Domain.Rules;

namespace Quiosques.Domain.Tests.Rules;

// RN03: a reserva deve ser feita com no mínimo 2 e no máximo 90 dias de antecedência.
public class BookingWindowTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    [Theory]
    [InlineData(2026, 10, 9, false)]   // hoje
    [InlineData(2026, 10, 10, false)]  // 1 dia
    [InlineData(2026, 10, 11, true)]   // 2 dias
    [InlineData(2027, 1, 7, true)]     // 90 dias
    [InlineData(2027, 1, 8, false)]    // 91 dias
    [InlineData(2026, 10, 1, false)]   // passado
    public void RN03_Contains(int year, int month, int day, bool expected)
    {
        Assert.Equal(expected, BookingWindow.Contains(new DateOnly(year, month, day), Today));
    }

    [Theory]
    [InlineData(2026, 11, 20, false, DayAvailability.Free)]
    [InlineData(2026, 11, 20, true, DayAvailability.Occupied)]
    [InlineData(2026, 10, 10, false, DayAvailability.Unavailable)]
    [InlineData(2026, 10, 10, true, DayAvailability.Unavailable)]
    public void DayAvailability_CombinesWindowAndReservations(int year, int month, int day, bool hasActiveReservation, DayAvailability expected)
    {
        Assert.Equal(expected, DayAvailabilityRule.For(new DateOnly(year, month, day), Today, hasActiveReservation));
    }
}
