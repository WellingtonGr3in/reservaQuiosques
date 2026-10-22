using Quiosques.Domain.Entities;

namespace Quiosques.Application.Reservations;

// Telas "Consulta de Reserva" e "Minhas reservas": dados do comprovante, foto do quiosque e até quando dá para cancelar (RN07).
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
