namespace Quiosques.Domain.Rules;

// Situação de um dia no calendário do quiosque (RF03).
public enum DayAvailability
{
    Free,
    Occupied,
    Unavailable,
}

public static class DayAvailabilityRule
{
    // Fora da janela da RN03 o dia não pode ser reservado, então aparece como indisponível
    // mesmo que exista reserva nele. Dentro da janela, uma reserva ativa (RN01) ocupa o dia.
    public static DayAvailability For(DateOnly date, DateOnly today, bool hasActiveReservation)
    {
        if (!BookingWindow.Contains(date, today))
        {
            return DayAvailability.Unavailable;
        }

        return hasActiveReservation ? DayAvailability.Occupied : DayAvailability.Free;
    }
}
