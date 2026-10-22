using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Domain.Entities;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Reservations;

public record CustomerReservationItem(ReservationDetails Details, bool Completed);

public record CustomerReservations(string HolderName, string MaskedCpf, IReadOnlyList<CustomerReservationItem> Reservations);

// HU05 / RF10: histórico de reservas do cliente logado ("Minhas reservas").
// HU04 / RF08: a consulta sem login usa a mesma lista, encontrada pelo CPF (RN10).
public class ListCustomerReservations(ICustomerRepository customers, IReservationRepository reservations, IClock clock)
{
    public const string NoReservationsMessage = "Não encontramos reservas com este CPF";

    public async Task<CustomerReservations> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Cliente não encontrado");
        return await ListAsync(customer, cancellationToken);
    }

    // RN10: sem login, as reservas são consultadas só com o CPF do titular (RN04).
    public async Task<CustomerReservations> ExecuteByCpfAsync(string? cpf, CancellationToken cancellationToken)
    {
        var parsed = Cpf.Parse(cpf);
        var customer = await customers.GetByCpfAsync(parsed.Value, cancellationToken)
            ?? throw new NotFoundException(NoReservationsMessage);
        return await ListAsync(customer, cancellationToken);
    }

    private async Task<CustomerReservations> ListAsync(Customer customer, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var today = clock.TodayInSaoPaulo();

        var items = (await reservations.ListByCustomerAsync(customer.Id, cancellationToken))
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
