using Quiosques.Domain.Entities;
using Quiosques.Domain.Rules;

namespace Quiosques.Domain.Tests.Rules;

// RN07: cancelamento até 48h antes das 00:00 do dia reservado (horário de Brasília).
public class CancellationPolicyTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);

    [Fact]
    public void RN07_DeadlineIsMidnightInBrasiliaMinus48Hours()
    {
        // 20/11 00:00 em Brasília (UTC-3) = 20/11 03:00 UTC; menos 48h = 18/11 03:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 3, 0, 0, TimeSpan.Zero), CancellationPolicy.DeadlineFor(Nov20));
    }

    [Theory]
    [InlineData(ReservationStatus.Confirmed, "2026-11-18T02:59:00Z", true)]
    [InlineData(ReservationStatus.Confirmed, "2026-11-18T03:00:00Z", true)]   // exatamente no limite
    [InlineData(ReservationStatus.Confirmed, "2026-11-18T03:00:01Z", false)]
    [InlineData(ReservationStatus.Cancelled, "2026-10-09T15:00:00Z", false)]
    [InlineData(ReservationStatus.Expired, "2026-10-09T15:00:00Z", false)]
    public void RN07_CanBeCancelledAt(ReservationStatus status, string now, bool expected)
    {
        var reservation = new Reservation("QSK-7F3K9Q", Guid.NewGuid(), Guid.NewGuid(), Nov20, 150m, status,
            new DateTimeOffset(2026, 10, 9, 15, 15, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 9, 15, 0, 0, TimeSpan.Zero));

        Assert.Equal(expected, reservation.CanBeCancelledAt(DateTimeOffset.Parse(now)));
    }

    [Fact]
    public void RN07_PendingReservationPastPaymentDeadlineCannotBeCancelled()
    {
        var created = new DateTimeOffset(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);
        var reservation = new Reservation("QSK-7F3K9Q", Guid.NewGuid(), Guid.NewGuid(), Nov20, 150m,
            ReservationStatus.PendingPayment, created.AddMinutes(15), created);

        Assert.True(reservation.CanBeCancelledAt(created.AddMinutes(14)));
        Assert.False(reservation.CanBeCancelledAt(created.AddMinutes(15)));
    }

    [Fact]
    public void BrazilTime_DateOfUsesBrasiliaTimeZone()
    {
        // 10/10 às 02:00 UTC ainda é 09/10 às 23:00 em Brasília.
        Assert.Equal(new DateOnly(2026, 10, 9), BrazilTime.DateOf(new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero)));
    }
}
