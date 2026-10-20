using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Domain.Entities;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Reservations;

public record CustomerReservationItem(ReservationDetails Details, bool Completed);

public record CustomerReservations(string HolderName, string MaskedCpf, IReadOnlyList<CustomerReservationItem> Reservations);

// HU05 / RF10: histórico de reservas do cliente logado ("Minhas reservas").
public class ListCustomerReservations(ICustomerRepository customers, IReservationRepository reservations, IClock clock)
{
    public async Task<CustomerReservations> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado");
        var now = clock.UtcNow;
        var today = clock.TodayInSaoPaulo();

        var items = (await reservations.ListByCustomerAsync(customerId, cancellationToken))
            .Select(r => new CustomerReservationItem(ReservationDetails.From(r, now), IsCompleted(r, today)))
            .ToList();

        return new CustomerReservations(customer.Name, Cpf.Parse(customer.Cpf).Masked, items);
    }

    // "Realizada" não é um status gravado: é uma reserva confirmada cuja data já passou (docs/05-modelagem-dados.md).
    private static bool IsCompleted(Reservation reservation, DateOnly today) =>
        reservation.Status == ReservationStatus.Confirmed && reservation.ReservationDate < today;
}

// "Ver comprovante" / "Ver detalhes" em "Minhas reservas". Só reservas do próprio cliente.
public class GetCustomerReservation(IReservationRepository reservations, IClock clock)
{
    public async Task<ReservationDetails> ExecuteAsync(Guid customerId, string code, CancellationToken cancellationToken)
    {
        var reservation = await reservations.GetByCodeAndCustomerAsync(code.Trim().ToUpperInvariant(), customerId, cancellationToken)
            ?? throw new NotFoundException(ReservationLookup.NotFoundMessage);
        return ReservationDetails.From(reservation, clock.UtcNow);
    }
}
