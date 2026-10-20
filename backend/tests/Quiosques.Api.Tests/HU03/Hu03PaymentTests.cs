using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU03;

// HU03 — Pagar e receber o comprovante (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu03PaymentTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);

    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu03PaymentTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_CobrancaPixGerada()
    {
        // Dado que tenho uma reserva aguardando pagamento
        var reservation = await CreateReservationAsync();

        // Quando eu abrir a tela de pagamento
        var payment = await StartPaymentAsync(reservation);

        // Então vejo o QR Code, o código copia-e-cola e o tempo restante
        Assert.False(string.IsNullOrWhiteSpace(payment.PixCopyPaste)); // o QR Code é gerado a partir deste código
        Assert.StartsWith("000201", payment.PixCopyPaste);                // BR Code (Pix estático)
        Assert.Contains("br.gov.bcb.pix", payment.PixCopyPaste);
        Assert.Contains($"54{reservation.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Length:D2}", payment.PixCopyPaste);
        Assert.Equal(TestData.Now.AddMinutes(15), payment.ExpiresAt);
        Assert.Equal(reservation.Amount, payment.Amount);
        Assert.Equal(reservation.Code, payment.ReservationCode);
    }

    [Fact]
    public async Task RF05_AbrirOPagamentoDeNovoReaproveitaACobranca()
    {
        var reservation = await CreateReservationAsync();

        var first = await StartPaymentAsync(reservation);
        var second = await StartPaymentAsync(reservation);

        Assert.Equal(first.TransactionId, second.TransactionId);
        Assert.Equal(first.PixCopyPaste, second.PixCopyPaste);
        await using var context = _fixture.CreateContext();
        Assert.Equal(1, await context.Payments.CountAsync(p => p.GatewayTransactionId == first.TransactionId));
    }

    [Fact]
    public async Task Cenario_PagamentoAprovado()
    {
        // Dado que tenho uma reserva aguardando pagamento
        var reservation = await CreateReservationAsync(email: "titular@exemplo.com");
        var payment = await StartPaymentAsync(reservation);

        // Quando o pagamento Pix for aprovado
        var outcome = await ApproveAsync(payment.TransactionId);

        // Então a reserva passa para "Confirmada"
        Assert.Equal(nameof(PaymentOutcome.Confirmed), outcome);
        var receipt = await GetReceiptAsync(reservation);
        Assert.Equal("CONFIRMADA", receipt.Status);

        // E recebo o comprovante por e-mail com o código da reserva e o CPF mascarado
        var (to, sent) = Assert.Single(_fixture.Emails.SentFor(reservation.Code));
        Assert.Equal("titular@exemplo.com", to);
        Assert.Equal(reservation.Code, sent.Code);
        Assert.Equal(MaskedCpf(reservation.Cpf), sent.MaskedCpf);
    }

    [Fact]
    public async Task Cenario_ConfirmacaoDePagamentoRepetida()
    {
        // Dado que minha reserva já foi confirmada
        var reservation = await CreateReservationAsync();
        var payment = await StartPaymentAsync(reservation);
        await ApproveAsync(payment.TransactionId);

        // Quando o gateway enviar de novo a mesma confirmação de pagamento
        var response = await NotifyAsync(payment.TransactionId);

        // Então a reserva continua "Confirmada"
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CONFIRMADA", (await GetReceiptAsync(reservation)).Status);

        // E nenhum novo comprovante é enviado
        Assert.Single(_fixture.Emails.SentFor(reservation.Code));
        await AssertPaymentStillApprovedAsync(payment.TransactionId);
    }

    [Fact]
    public async Task RNF03_NotificacoesSimultaneasConfirmamUmaSoVez()
    {
        var reservation = await CreateReservationAsync();
        var payment = await StartPaymentAsync(reservation);
        _fixture.Gateway.Approve(payment.TransactionId);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => NotifyAsync(payment.TransactionId)));

        Assert.Equal("CONFIRMADA", (await GetReceiptAsync(reservation)).Status);
        Assert.Single(_fixture.Emails.SentFor(reservation.Code));
        await AssertPaymentStillApprovedAsync(payment.TransactionId);
    }

    // A notificação repetida não pode cair no caminho do RN11 e reembolsar um pagamento válido.
    private async Task AssertPaymentStillApprovedAsync(string transactionId)
    {
        Assert.Equal(GatewayPaymentStatus.Approved, _fixture.Gateway.StatusOf(transactionId));
        await using var context = _fixture.CreateContext();
        Assert.Equal(PaymentStatus.Approved, (await context.Payments.SingleAsync(p => p.GatewayTransactionId == transactionId)).Status);
    }

    [Fact]
    public async Task RF06_NotificacaoDePagamentoNaoAprovadoNaoConfirma()
    {
        var reservation = await CreateReservationAsync();
        var payment = await StartPaymentAsync(reservation);

        // O gateway ainda não aprovou: a notificação sozinha não basta.
        await NotifyAsync(payment.TransactionId);

        Assert.Equal("PENDENTE_PAGAMENTO", (await GetReceiptAsync(reservation)).Status);
        Assert.Empty(_fixture.Emails.SentFor(reservation.Code));
    }

    [Fact]
    public async Task Cenario_PagamentoNaoRealizadoNoPrazo()
    {
        // Dado que tenho uma reserva aguardando pagamento há 15 minutos
        var reservation = await CreateReservationAsync();
        _fixture.Clock.UtcNow = TestData.Now.AddMinutes(15);

        // Quando o prazo terminar sem pagamento
        await _fixture.ExpireOverdueReservationsAsync();

        // Então a reserva passa para "Expirada"
        Assert.Equal("EXPIRADA", (await GetReceiptAsync(reservation)).Status);

        // E a data volta a ficar livre no calendário
        Assert.Equal("LIVRE", await CalendarStatusAsync(reservation.KioskId, Nov20));
    }

    [Fact]
    public async Task RN05_ReservaDentroDoPrazoNaoExpira()
    {
        var reservation = await CreateReservationAsync();
        _fixture.Clock.UtcNow = TestData.Now.AddMinutes(14);

        await _fixture.ExpireOverdueReservationsAsync();

        Assert.Equal("PENDENTE_PAGAMENTO", (await GetReceiptAsync(reservation)).Status);
    }

    [Fact]
    public async Task Cenario_PagamentoAprovadoDepoisDoPrazo()
    {
        // Dado que minha reserva expirou
        var reservation = await CreateReservationAsync();
        var payment = await StartPaymentAsync(reservation);
        _fixture.Clock.UtcNow = TestData.Now.AddMinutes(15);
        await _fixture.ExpireOverdueReservationsAsync();

        // Quando o pagamento Pix dessa reserva for aprovado
        var outcome = await ApproveAsync(payment.TransactionId);

        // Então o valor é reembolsado automaticamente
        Assert.Equal(nameof(PaymentOutcome.Refunded), outcome);
        Assert.Equal(GatewayPaymentStatus.Refunded, _fixture.Gateway.StatusOf(payment.TransactionId));
        await using var context = _fixture.CreateContext();
        Assert.Equal(PaymentStatus.Refunded, (await context.Payments.SingleAsync(p => p.GatewayTransactionId == payment.TransactionId)).Status);

        // E a reserva continua "Expirada"
        Assert.Equal("EXPIRADA", (await GetReceiptAsync(reservation)).Status);
        Assert.Empty(_fixture.Emails.SentFor(reservation.Code));
    }

    [Fact]
    public async Task RN05_ReservaExpiradaNaoGeraCobranca()
    {
        var reservation = await CreateReservationAsync();
        _fixture.Clock.UtcNow = TestData.Now.AddMinutes(15);

        var response = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/pagamento", new { cpf = reservation.Cpf });

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "Esta reserva não está aguardando pagamento");
    }

    [Fact]
    public async Task RN10_PagamentoEComprovanteExigemOCpfDaReserva()
    {
        var reservation = await CreateReservationAsync();
        var otherCpf = TestData.NextCpf();

        var payment = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/pagamento", new { cpf = otherCpf });
        var receipt = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/comprovante", new { cpf = otherCpf });
        var invalidCpf = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/comprovante", new { cpf = "123" });

        await AssertErrorAsync(payment, HttpStatusCode.NotFound, "Reserva não encontrada");
        await AssertErrorAsync(receipt, HttpStatusCode.NotFound, "Reserva não encontrada");
        await AssertErrorAsync(invalidCpf, HttpStatusCode.NotFound, "Reserva não encontrada");
    }

    [Fact]
    public async Task Comprovante_MostraOsDadosDaTelaComCpfMascarado()
    {
        var kiosk = await _data.KioskAsync(name: $"Quiosque Comprovante {Guid.NewGuid():N}");
        var reservation = await CreateReservationAsync(kiosk);
        var payment = await StartPaymentAsync(reservation);
        await ApproveAsync(payment.TransactionId);

        var receipt = await GetReceiptAsync(reservation);

        Assert.Equal(reservation.Code, receipt.Code);
        Assert.Equal(kiosk.Name, receipt.KioskName);
        Assert.Equal(kiosk.Capacity, receipt.KioskCapacity);
        Assert.Equal(Nov20, receipt.Date);
        Assert.Equal(new TimeOnly(8, 0), receipt.StartTime); // RN02
        Assert.Equal(new TimeOnly(22, 0), receipt.EndTime);
        Assert.Equal("João da Silva", receipt.HolderName);
        Assert.Equal(MaskedCpf(reservation.Cpf), receipt.MaskedCpf); // RNF07
        Assert.Equal(kiosk.DailyRate, receipt.Amount);
        Assert.Equal("CONFIRMADA", receipt.Status);
    }

    private async Task<CreatedReservation> CreateReservationAsync(Kiosk? kiosk = null, string email = "joao@exemplo.com")
    {
        kiosk ??= await _data.KioskAsync();
        var cpf = TestData.NextCpf();
        var response = await _client.PostAsJsonAsync("/api/reservas", new
        {
            kioskId = kiosk.Id, date = Nov20, cpf, name = "João da Silva", email, phone = "47999998888",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        return new CreatedReservation(body.Code, cpf, kiosk.Id, body.Amount);
    }

    private async Task<PixPaymentResponse> StartPaymentAsync(CreatedReservation reservation)
    {
        var response = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/pagamento", new { cpf = reservation.Cpf });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PixPaymentResponse>())!;
    }

    private async Task<string> ApproveAsync(string transactionId)
    {
        var response = await _client.PostAsync($"/api/dev/pagamentos/{transactionId}/aprovar", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OutcomeResponse>())!.Outcome;
    }

    private Task<HttpResponseMessage> NotifyAsync(string transactionId) =>
        _client.PostAsJsonAsync("/api/pagamentos/notificacao", new { transactionId });

    private async Task<ReceiptResponse> GetReceiptAsync(CreatedReservation reservation)
    {
        var response = await _client.PostAsJsonAsync($"/api/reservas/{reservation.Code}/comprovante", new { cpf = reservation.Cpf });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReceiptResponse>())!;
    }

    private async Task<string> CalendarStatusAsync(Guid kioskId, DateOnly date)
    {
        var calendar = await _client.GetFromJsonAsync<CalendarResponse>($"/api/quiosques/{kioskId}/calendario?ano={date.Year}&mes={date.Month}");
        return calendar!.Days.Single(d => d.Date == date).Status;
    }

    private static string MaskedCpf(string digits) => $"***.{digits[3..6]}.{digits[6..9]}-**";

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(message, (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    private record CreatedReservation(string Code, string Cpf, Guid KioskId, decimal Amount);

    private record ReservationResponse(string Code, decimal Amount);

    private record PixPaymentResponse(string ReservationCode, decimal Amount, string PixCopyPaste, DateTimeOffset ExpiresAt, string TransactionId);

    private record OutcomeResponse(string Outcome);

    private record ReceiptResponse(
        string Code, string KioskName, int KioskCapacity, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
        string HolderName, string MaskedCpf, decimal Amount, string Status);

    private record ErrorResponse(string Message);

    private record CalendarResponse(List<CalendarDayResponse> Days);

    private record CalendarDayResponse(DateOnly Date, string Status);
}
