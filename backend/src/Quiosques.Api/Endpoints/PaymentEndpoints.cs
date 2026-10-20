using Quiosques.Application.Payments;
using Quiosques.Infrastructure.Payments;

namespace Quiosques.Api.Endpoints;

// HU03: pagar via Pix.
public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment environment)
    {
        // RF05: gera (ou reaproveita) a cobrança Pix. Código + CPF, como na consulta sem login (RN10).
        app.MapPost("/api/reservas/{code}/pagamento", async (string code, CpfRequest request, StartPayment useCase, CancellationToken ct) =>
        {
            var payment = await useCase.ExecuteAsync(code, request.Cpf, ct);
            return Results.Ok(new PixPaymentResponse(
                payment.ReservationCode, payment.Amount, payment.PixCopyPaste, payment.ExpiresAt, payment.TransactionId));
        });

        // RF06: notificação do gateway. A situação é conferida no próprio gateway antes de confirmar.
        app.MapPost("/api/pagamentos/notificacao", async (PaymentNotification notification, ConfirmPayment useCase, CancellationToken ct) =>
        {
            await useCase.ExecuteAsync(notification.TransactionId, ct);
            return Results.Ok();
        });

        if (environment.IsDevelopment())
        {
            // Só em desenvolvimento: simula o cliente pagando o Pix e o gateway enviando a notificação.
            app.MapPost("/api/dev/pagamentos/{transactionId}/aprovar",
                async (string transactionId, FakePaymentGateway gateway, ConfirmPayment useCase, CancellationToken ct) =>
                {
                    gateway.Approve(transactionId);
                    var outcome = await useCase.ExecuteAsync(transactionId, ct);
                    return Results.Ok(new { outcome = outcome.ToString() });
                });
        }

        return app;
    }

    public record CpfRequest(string? Cpf);

    public record PaymentNotification(string TransactionId);

    public record PixPaymentResponse(string ReservationCode, decimal Amount, string PixCopyPaste, DateTimeOffset ExpiresAt, string TransactionId);
}
