namespace Quiosques.Domain.Rules;

// RN08: cada CPF pode ter no máximo 2 reservas ativas ao mesmo tempo.
public static class CustomerReservationLimit
{
    public const int MaxActiveReservations = 2;
    public const string LimitReachedMessage = "Limite de 2 reservas ativas por CPF atingido";

    public static void EnsureCanReserve(int activeReservations)
    {
        if (activeReservations >= MaxActiveReservations)
        {
            throw new DomainException(LimitReachedMessage);
        }
    }
}
