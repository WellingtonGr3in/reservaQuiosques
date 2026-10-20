using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU04;

// HU04 — Consultar e cancelar reserva (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu04LookupAndCancelTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);

    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu04LookupAndCancelTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.CreateClientWithOwnIp();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_ConsultaComDadosCorretos()
    {
        var reservation = await CreateConfirmedReservationAsync(Nov20);

        // Quando eu informar o código e o CPF da reserva
        var response = await LookupAsync(reservation.Code, TestData.FormatCpf(reservation.Cpf));

        // Então vejo os dados da reserva
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var details = (await response.Content.ReadFromJsonAsync<DetailsResponse>())!;
        Assert.Equal(reservation.Code, details.Code);
        Assert.Equal(Nov20, details.Date);
        Assert.Equal("CONFIRMADA", details.Status);
        Assert.Equal("João da Silva", details.HolderName);
        Assert.Equal($"***.{reservation.Cpf[3..6]}.{reservation.Cpf[6..9]}-**", details.MaskedCpf); // RNF07
        // RN07: para 20/11, o prazo vai até 18/11 às 00:00 de Brasília (03:00 UTC).
        Assert.Equal(new DateTimeOffset(2026, 11, 18, 3, 0, 0, TimeSpan.Zero), details.CancellationDeadline);
        Assert.True(details.CanCancel);
    }

    [Fact]
    public async Task Cenario_CodigoECpfNaoBatem()
    {
        var reservation = await CreateConfirmedReservationAsync(Nov20);

        // Quando eu informar um código com um CPF diferente do da reserva
        var response = await LookupAsync(reservation.Code, TestData.NextCpf());

        // Então o sistema informa "Reserva não encontrada"
        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Reserva não encontrada");
    }

    [Fact]
    public async Task Cenario_MuitasTentativasDeConsulta()
    {
        // Dado que fiz 5 consultas no último minuto
        for (var i = 0; i < 5; i++)
        {
            var attempt = await LookupAsync("QSK-AAAAAA", TestData.NextCpf());
            Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        }

        // Quando eu fizer a 6ª consulta
        var response = await LookupAsync("QSK-AAAAAA", TestData.NextCpf());

        // Então o sistema informa que devo aguardar antes de tentar novamente
        await AssertErrorAsync(response, HttpStatusCode.TooManyRequests, "Muitas tentativas. Aguarde um minuto e tente novamente.");
    }

    [Fact]
    public async Task RNF06_LimiteEPorIp()
    {
        for (var i = 0; i < 6; i++)
        {
            await LookupAsync("QSK-AAAAAA", TestData.NextCpf());
        }

        var otherIp = _fixture.CreateClientWithOwnIp();
        var response = await otherIp.PostAsJsonAsync("/api/reservas/consulta", new { code = "QSK-AAAAAA", cpf = TestData.NextCpf() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RNF06_CancelamentoContaNoMesmoLimite()
    {
        for (var i = 0; i < 5; i++)
        {
            await LookupAsync("QSK-AAAAAA", TestData.NextCpf());
        }

        var response = await CancelAsync("QSK-AAAAAA", TestData.NextCpf());

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Cenario_CancelamentoDentroDoPrazo()
    {
        // Dado que minha reserva confirmada é daqui a 10 dias
        var date = new DateOnly(2026, 10, 19);
        var reservation = await CreateConfirmedReservationAsync(date);

        // Quando eu solicitar o cancelamento
        var response = await CancelAsync(reservation.Code, reservation.Cpf);

        // Então a reserva passa para "Cancelada", o valor é reembolsado e a data é liberada
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELADA", (await response.Content.ReadFromJsonAsync<DetailsResponse>())!.Status);
        Assert.Equal(GatewayPaymentStatus.Refunded, _fixture.Gateway.StatusOf(reservation.TransactionId!));
        await using var context = _fixture.CreateContext();
        Assert.Equal(PaymentStatus.Refunded, (await context.Payments.SingleAsync(p => p.GatewayTransactionId == reservation.TransactionId)).Status);
        Assert.Equal("LIVRE", await CalendarStatusAsync(reservation.KioskId, date));
    }

    [Fact]
    public async Task Cenario_CancelamentoNoLimiteDoPrazo()
    {
        // Dado que minha reserva confirmada é para 20/11/2026
        var reservation = await CreateConfirmedReservationAsync(Nov20);

        // E agora são 17/11/2026 às 23:59 (Brasília, UTC-3)
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 11, 18, 2, 59, 0, TimeSpan.Zero);

        // Quando eu solicitar o cancelamento
        var response = await CancelAsync(reservation.Code, reservation.Cpf);

        // Então a reserva passa para "Cancelada"
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELADA", (await response.Content.ReadFromJsonAsync<DetailsResponse>())!.Status);
    }

    [Fact]
    public async Task RN07_UmMinutoDepoisDoLimiteNaoCancela()
    {
        var reservation = await CreateConfirmedReservationAsync(Nov20);
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 11, 18, 3, 1, 0, TimeSpan.Zero); // 18/11 às 00:01 em Brasília

        var response = await CancelAsync(reservation.Code, reservation.Cpf);

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "O cancelamento só é permitido até 48h antes da data reservada");
    }

    [Fact]
    public async Task Cenario_CancelamentoForaDoPrazo()
    {
        // Dado que minha reserva confirmada é daqui a 1 dia
        var reservation = await CreateConfirmedReservationAsync(Nov20);
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 11, 19, 15, 0, 0, TimeSpan.Zero);

        // Quando eu solicitar o cancelamento
        var response = await CancelAsync(reservation.Code, reservation.Cpf);

        // Então o sistema informa que o cancelamento só é permitido até 48h antes
        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "O cancelamento só é permitido até 48h antes da data reservada");
        Assert.Equal("CONFIRMADA", await StatusAsync(reservation));
    }

    [Fact]
    public async Task Cenario_CancelamentoDeReservaAguardandoPagamento()
    {
        // Dado que tenho uma reserva aguardando pagamento
        var reservation = await CreateReservationAsync(Nov20);
        var payment = await StartPaymentAsync(reservation);

        // Quando eu solicitar o cancelamento
        var response = await CancelAsync(reservation.Code, reservation.Cpf);

        // Então a reserva passa para "Cancelada", sem reembolso, e a data é liberada
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELADA", (await response.Content.ReadFromJsonAsync<DetailsResponse>())!.Status);
        Assert.Equal(GatewayPaymentStatus.Pending, _fixture.Gateway.StatusOf(payment));
        Assert.Equal("LIVRE", await CalendarStatusAsync(reservation.KioskId, Nov20));
    }

    [Fact]
    public async Task RN11_PixPagoDepoisDoCancelamentoEReembolsado()
    {
        var reservation = await CreateReservationAsync(Nov20);
        var payment = await StartPaymentAsync(reservation);
        await CancelAsync(reservation.Code, reservation.Cpf);

        var outcome = await ApproveAsync(payment);

        Assert.Equal(nameof(PaymentOutcome.Refunded), outcome);
        Assert.Equal("CANCELADA", await StatusAsync(reservation));
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Expired)]
    public async Task RN07_ReservaInativaNaoPodeSerCancelada(ReservationStatus status)
    {
        var kiosk = await _data.KioskAsync();
        var customer = await _data.CustomerAsync();
        var reservation = await _data.AddAsync(new Reservation(
            TestData.NextCode(), customer.Id, kiosk.Id, Nov20, 150m, status, TestData.Now, TestData.Now));

        var response = await CancelAsync(reservation.Code, customer.Cpf);

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "Esta reserva não pode ser cancelada");
    }

    [Fact]
    public async Task RN10_CancelamentoComCpfErradoNaoEncontraAReserva()
    {
        var reservation = await CreateConfirmedReservationAsync(Nov20);

        var response = await CancelAsync(reservation.Code, TestData.NextCpf());

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Reserva não encontrada");
        Assert.Equal("CONFIRMADA", await StatusAsync(reservation));
    }

    private async Task<CreatedReservation> CreateReservationAsync(DateOnly date)
    {
        var kiosk = await _data.KioskAsync();
        var cpf = TestData.NextCpf();
        var response = await _fixture.Api.CreateClient().PostAsJsonAsync("/api/reservas", new
        {
            kioskId = kiosk.Id, date, cpf, name = "João da Silva", email = "joao@exemplo.com", phone = "47999998888",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var code = (await response.Content.ReadFromJsonAsync<CodeResponse>())!.Code;
        return new CreatedReservation(code, cpf, kiosk.Id, null);
    }

    private async Task<CreatedReservation> CreateConfirmedReservationAsync(DateOnly date)
    {
        var reservation = await CreateReservationAsync(date);
        var transactionId = await StartPaymentAsync(reservation);
        Assert.Equal(nameof(PaymentOutcome.Confirmed), await ApproveAsync(transactionId));
        return reservation with { TransactionId = transactionId };
    }

    private async Task<string> StartPaymentAsync(CreatedReservation reservation)
    {
        var response = await _fixture.Api.CreateClient().PostAsJsonAsync($"/api/reservas/{reservation.Code}/pagamento", new { cpf = reservation.Cpf });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PaymentResponse>())!.TransactionId;
    }

    private async Task<string> ApproveAsync(string transactionId)
    {
        var response = await _fixture.Api.CreateClient().PostAsync($"/api/dev/pagamentos/{transactionId}/aprovar", null);
        return (await response.Content.ReadFromJsonAsync<OutcomeResponse>())!.Outcome;
    }

    private Task<HttpResponseMessage> LookupAsync(string code, string cpf) =>
        _client.PostAsJsonAsync("/api/reservas/consulta", new { code, cpf });

    private Task<HttpResponseMessage> CancelAsync(string code, string cpf) =>
        _client.PostAsJsonAsync($"/api/reservas/{code}/cancelamento", new { cpf });

    // Lê a situação direto do banco, sem gastar tentativas do limite de consulta.
    private async Task<string> StatusAsync(CreatedReservation reservation)
    {
        await using var context = _fixture.CreateContext();
        var status = (await context.Reservations.SingleAsync(r => r.Code == reservation.Code)).Status;
        return status switch
        {
            ReservationStatus.PendingPayment => "PENDENTE_PAGAMENTO",
            ReservationStatus.Confirmed => "CONFIRMADA",
            ReservationStatus.Expired => "EXPIRADA",
            _ => "CANCELADA",
        };
    }

    private async Task<string> CalendarStatusAsync(Guid kioskId, DateOnly date)
    {
        var calendar = await _client.GetFromJsonAsync<CalendarResponse>($"/api/quiosques/{kioskId}/calendario?ano={date.Year}&mes={date.Month}");
        return calendar!.Days.Single(d => d.Date == date).Status;
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(message, (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    private record CreatedReservation(string Code, string Cpf, Guid KioskId, string? TransactionId);

    private record CodeResponse(string Code);

    private record PaymentResponse(string TransactionId);

    private record OutcomeResponse(string Outcome);

    private record DetailsResponse(
        string Code, string KioskName, int KioskCapacity, string KioskPhotoUrl, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
        string HolderName, string MaskedCpf, decimal Amount, string Status, DateTimeOffset CreatedAt,
        DateTimeOffset CancellationDeadline, bool CanCancel);

    private record ErrorResponse(string Message);

    private record CalendarResponse(List<CalendarDayResponse> Days);

    private record CalendarDayResponse(DateOnly Date, string Status);
}
