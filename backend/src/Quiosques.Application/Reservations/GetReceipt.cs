namespace Quiosques.Application.Reservations;

// Tela "Comprovante de Reserva" (HU03). Também usada pela tela de pagamento para saber quando o Pix foi aprovado.
public class GetReceipt(IReservationRepository reservations)
{
    public async Task<ReservationReceipt> ExecuteAsync(string? code, string? cpf, CancellationToken cancellationToken) =>
        ReservationReceipt.From(await ReservationLookup.FindAsync(reservations, code, cpf, cancellationToken));
}
