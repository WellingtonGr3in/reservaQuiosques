using Quiosques.Domain.Entities;
using Quiosques.Domain.Rules;
using Quiosques.Domain.ValueObjects;

namespace Quiosques.Application.Reservations;

// Dados do comprovante (tela "Comprovante de Reserva" e e-mail do RF07). O CPF vai mascarado (RNF07).
public record ReservationReceipt(
    string Code,
    string KioskName,
    int KioskCapacity,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string HolderName,
    string MaskedCpf,
    decimal Amount,
    ReservationStatus Status)
{
    // Exige a reserva carregada com quiosque e cliente.
    public static ReservationReceipt From(Reservation reservation) => new(
        reservation.Code,
        reservation.Kiosk.Name,
        reservation.Kiosk.Capacity,
        reservation.ReservationDate,
        DailyUsage.Start,
        DailyUsage.End,
        reservation.Customer.Name,
        Cpf.Parse(reservation.Customer.Cpf).Masked,
        reservation.Amount,
        reservation.Status);
}
