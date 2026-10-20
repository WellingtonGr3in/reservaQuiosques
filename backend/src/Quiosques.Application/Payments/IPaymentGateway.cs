namespace Quiosques.Application.Payments;

// Cobrança Pix: o código copia-e-cola também é o conteúdo do QR Code (RF05).
public record PixCharge(string TransactionId, string CopyPaste);

public enum GatewayPaymentStatus
{
    Pending,
    Approved,
    Refunded,
}

// Gateway de pagamento Pix. Primeiro FakePaymentGateway; o gateway real entra na etapa 8 (CLAUDE.md).
public interface IPaymentGateway
{
    Task<PixCharge> CreatePixChargeAsync(string reservationCode, decimal amount, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    Task<PixCharge> GetPixChargeAsync(string transactionId, CancellationToken cancellationToken);

    // A situação é sempre consultada no gateway: a notificação recebida não é confiável por si só.
    Task<GatewayPaymentStatus> GetStatusAsync(string transactionId, CancellationToken cancellationToken);

    Task RefundAsync(string transactionId, CancellationToken cancellationToken);
}
