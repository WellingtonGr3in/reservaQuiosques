using Quiosques.Domain.Entities;
using Quiosques.Domain.Rules;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Domain.Tests.Entities;

public class ReservationTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 15, 0, 0, TimeSpan.Zero);
    private static readonly Customer Customer = new("52998224725", "João", "joao@exemplo.com", "47999998888", Now);
    private static readonly Kiosk Kiosk = new("Quiosque A", "Descrição", 30, 150m, "/a.jpg", "Regras", isActive: true);

    [Fact]
    public void HU02_Create_StartsPendingWithDailyRateAndFifteenMinuteDeadline()
    {
        var reservation = Reservation.Create("QSK-7F3K9Q", Customer, Kiosk, new DateOnly(2026, 11, 20), Today, Now);

        Assert.Equal(ReservationStatus.PendingPayment, reservation.Status);
        Assert.Equal(150m, reservation.Amount);
        Assert.Equal(Now.AddMinutes(15), reservation.ExpiresAt); // RN05
        Assert.Equal(Customer.Id, reservation.CustomerId);
        Assert.Equal(Kiosk.Id, reservation.KioskId);
    }

    [Theory]
    [InlineData(2026, 10, 10)]
    [InlineData(2027, 1, 8)]
    public void RN03_Create_OutsideBookingWindow_Throws(int year, int month, int day)
    {
        var exception = Assert.Throws<DomainException>(
            () => Reservation.Create("QSK-7F3K9Q", Customer, Kiosk, new DateOnly(year, month, day), Today, Now));

        Assert.Equal("A reserva deve ser feita com 2 a 90 dias de antecedência", exception.Message);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void RN08_CustomerReservationLimit(int activeReservations, bool allowed)
    {
        var exception = Record.Exception(() => CustomerReservationLimit.EnsureCanReserve(activeReservations));

        Assert.Equal(allowed, exception is null);
    }

    [Fact]
    public void ReservationCode_FollowsDerFormat()
    {
        for (var i = 0; i < 1000; i++)
        {
            Assert.Matches("^QSK-[A-HJ-NP-Z2-9]{6}$", ReservationCode.Generate());
        }
    }

    [Fact]
    public void RN12_UpdateContact_ReplacesNameEmailAndPhone()
    {
        var customer = new Customer("52998224725", "João", "antigo@exemplo.com", "47999998888", Now);

        customer.UpdateContact("João Silva", "novo@exemplo.com", "47988887777");

        Assert.Equal(("João Silva", "novo@exemplo.com", "47988887777"), (customer.Name, customer.Email, customer.Phone));
    }
}
