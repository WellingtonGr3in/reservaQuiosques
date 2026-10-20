using Quiosques.Application.Common;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Rules;

namespace Quiosques.Application.Kiosks;

public record CalendarDay(DateOnly Date, DayAvailability Availability);

public record KioskCalendar(Guid KioskId, int Year, int Month, IReadOnlyList<CalendarDay> Days);

// RF03: calendário mensal com as datas livres e ocupadas do quiosque (RN01, RN03, RN05).
public class GetKioskCalendar(IKioskRepository kiosks, IReservationRepository reservations, IClock clock)
{
    public async Task<KioskCalendar?> ExecuteAsync(Guid kioskId, int year, int month, CancellationToken cancellationToken)
    {
        if (!await kiosks.IsActiveAsync(kioskId, cancellationToken))
        {
            return null;
        }

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = clock.TodayInSaoPaulo();

        var occupied = await reservations.GetActiveReservationDatesAsync(
            kioskId, firstDay, lastDay, clock.UtcNow, cancellationToken);

        var days = Enumerable.Range(0, lastDay.Day)
            .Select(offset => firstDay.AddDays(offset))
            .Select(date => new CalendarDay(date, DayAvailabilityRule.For(date, today, occupied.Contains(date))))
            .ToList();

        return new KioskCalendar(kioskId, year, month, days);
    }
}
