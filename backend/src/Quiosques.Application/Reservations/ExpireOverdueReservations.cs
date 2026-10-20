using Quiosques.Application.Common;

namespace Quiosques.Application.Reservations;

// RN05: reservas aguardando pagamento há mais de 15 minutos passam para EXPIRADA e liberam a data.
public class ExpireOverdueReservations(IReservationRepository reservations, IClock clock)
{
    public Task<int> ExecuteAsync(CancellationToken cancellationToken) =>
        reservations.ExpireAllOverduePendingAsync(clock.UtcNow, cancellationToken);
}
