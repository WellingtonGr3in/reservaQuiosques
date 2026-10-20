using Quiosques.Application.Common;
using Quiosques.Domain.Entities;

namespace Quiosques.Application.Reservations;

// Tela "Consulta de Reserva": dados do comprovante, foto do quiosque e até quando dá para cancelar (RN07).
public record ReservationDetails(
    ReservationReceipt Receipt,
    string KioskPhotoUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset CancellationDeadline,
    bool CanCancel)
{
    // Exige a reserva carregada com quiosque e cliente.
    public static ReservationDetails From(Reservation reservation, DateTimeOffset now) => new(
        ReservationReceipt.From(reservation),
        reservation.Kiosk.PhotoUrl,
        reservation.CreatedAt,
        reservation.CancellationDeadline,
        reservation.CanBeCancelledAt(now));
}

// RF08 / HU04: consultar a reserva com código + CPF (RN10).
public class GetReservationDetails(IReservationRepository reservations, IClock clock)
{
    public async Task<ReservationDetails> ExecuteAsync(string? code, string? cpf, CancellationToken cancellationToken) =>
        ReservationDetails.From(await ReservationLookup.FindAsync(reservations, code, cpf, cancellationToken), clock.UtcNow);
}
