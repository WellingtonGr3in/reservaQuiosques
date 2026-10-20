using Quiosques.Application.Reservations;

namespace Quiosques.Application.Notifications;

public interface IEmailSender
{
    // RF07: comprovante com o código da reserva. O CPF vai mascarado (RNF07).
    Task SendReservationReceiptAsync(string to, ReservationReceipt receipt, CancellationToken cancellationToken);
}
