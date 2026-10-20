using Microsoft.Extensions.Logging;
using Quiosques.Application.Notifications;
using Quiosques.Application.Reservations;

namespace Quiosques.Infrastructure.Notifications;

// Envio de e-mail simulado: registra o comprovante no log. O CPF já vem mascarado (RNF07).
public partial class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendReservationReceiptAsync(string to, ReservationReceipt receipt, CancellationToken cancellationToken)
    {
        LogReceipt(logger, MaskEmail(to), receipt.Code, receipt.KioskName, receipt.Date, receipt.HolderName, receipt.MaskedCpf, receipt.Amount);
        return Task.CompletedTask;
    }

    // O e-mail também é dado pessoal: no log aparece só a primeira letra e o domínio.
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? $"{email[0]}***{email[at..]}" : "***";
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Comprovante enviado para {To}: reserva {Code}, {Kiosk}, {Date}, titular {Holder}, CPF {Cpf}, valor {Amount}")]
    private static partial void LogReceipt(
        ILogger logger, string to, string code, string kiosk, DateOnly date, string holder, string cpf, decimal amount);
}
