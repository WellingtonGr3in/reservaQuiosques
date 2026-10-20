using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Options;
using Quiosques.Application.Payments;

namespace Quiosques.Infrastructure.Payments;

// Gateway simulado (CLAUDE.md): guarda as cobranças em memória. A aprovação é feita pelo
// endpoint de desenvolvimento POST /api/dev/pagamentos/{transactionId}/aprovar.
// Com a chave Pix configurada (seção "Pix"), o copia-e-cola é um Pix estático real (PixBrCode):
// dá para pagar com o app do banco, mas a confirmação continua simulada.
// Como a memória se perde ao reiniciar a API, uma cobrança desconhecida é tratada como existente;
// o id carrega o código da reserva e o valor para o mesmo Pix poder ser gerado de novo.
public class FakePaymentGateway(IOptions<PixSettings> pixOptions) : IPaymentGateway
{
    private readonly ConcurrentDictionary<string, FakeCharge> _charges = new();

    public Task<PixCharge> CreatePixChargeAsync(string reservationCode, decimal amount, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var reference = new string(reservationCode.Where(char.IsAsciiLetterOrDigit).ToArray());
        var cents = decimal.ToInt64(decimal.Round(amount * 100));
        var transactionId = $"fake-{reference}-{cents}-{Guid.NewGuid():N}";
        _charges[transactionId] = new FakeCharge(CopyPaste(transactionId), GatewayPaymentStatus.Pending);
        return Task.FromResult(new PixCharge(transactionId, _charges[transactionId].CopyPaste));
    }

    public Task<PixCharge> GetPixChargeAsync(string transactionId, CancellationToken cancellationToken) =>
        Task.FromResult(new PixCharge(transactionId, Get(transactionId).CopyPaste));

    public Task<GatewayPaymentStatus> GetStatusAsync(string transactionId, CancellationToken cancellationToken) =>
        Task.FromResult(Get(transactionId).Status);

    public Task RefundAsync(string transactionId, CancellationToken cancellationToken)
    {
        _charges[transactionId] = Get(transactionId) with { Status = GatewayPaymentStatus.Refunded };
        return Task.CompletedTask;
    }

    // Simula o cliente pagando o Pix.
    public void Approve(string transactionId) =>
        _charges[transactionId] = Get(transactionId) with { Status = GatewayPaymentStatus.Approved };

    public GatewayPaymentStatus? StatusOf(string transactionId) =>
        _charges.TryGetValue(transactionId, out var charge) ? charge.Status : null;

    private FakeCharge Get(string transactionId) =>
        _charges.GetOrAdd(transactionId, id => new FakeCharge(CopyPaste(id), GatewayPaymentStatus.Pending));

    // Id no formato fake-{código sem hífen}-{valor em centavos}-{guid}.
    private string CopyPaste(string transactionId)
    {
        var parts = transactionId.Split('-');
        var settings = pixOptions.Value;
        if (!settings.IsConfigured || parts.Length != 4 || !long.TryParse(parts[2], out var cents))
        {
            return $"PIX-SIMULADO|{transactionId}";
        }

        return PixBrCode.Build(settings, cents / 100m, parts[1]);
    }

    private sealed record FakeCharge(string CopyPaste, GatewayPaymentStatus Status);
}
