using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Application.Payments;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU06;

// HU06 — Administrar reservas (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu06AdminReservationsTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);
    private static readonly DateOnly Nov21 = new(2026, 11, 21);
    private static readonly DateOnly Tomorrow = new(2026, 10, 10);

    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu06AdminReservationsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_LoginDoAdministrador()
    {
        // Quando ele entrar com o e-mail e a senha corretos
        var response = await _client.PostAsJsonAsync("/api/admin/login", new { email = PostgresFixture.AdminEmail, password = PostgresFixture.AdminPassword });

        // Então acessa a área de administração
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        Assert.Equal("Admin de Teste", token.Name);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/admin/quiosques", token.Token)).StatusCode);
    }

    [Fact]
    public async Task RF11_SenhaErradaNaoEntra()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/login", new { email = PostgresFixture.AdminEmail, password = "errada-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Cenario_ClienteNaoAcessaAAdministracao()
    {
        // Dado que estou logado como cliente
        var cpf = TestData.NextCpf();
        await CreateCustomerReservationAsync(await _data.KioskAsync(), Nov20, cpf);
        var email = $"cliente-{Guid.NewGuid():N}@exemplo.com";
        var account = await _client.PostAsJsonAsync("/api/contas", new { cpf, email, password = "senha-cliente-123" });
        var customerToken = (await account.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        // Quando eu tentar acessar a lista de reservas da administração
        var response = await SendAsync(HttpMethod.Post, "/api/admin/reservas/busca", customerToken, new { });

        // Então o acesso é negado
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/admin/reservas/busca", new { })).StatusCode);
    }

    [Fact]
    public async Task RF11_AdministradorNaoUsaAreaDoCliente()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/minhas-reservas", await AdminTokenAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cenario_ListarEFiltrarReservas()
    {
        // Dado que existem reservas no Quiosque A e no Quiosque B
        var kioskA = await _data.KioskAsync();
        var kioskB = await _data.KioskAsync();
        var inA = await CreateCustomerReservationAsync(kioskA, Nov20, TestData.NextCpf());
        await CreateCustomerReservationAsync(kioskB, Nov20, TestData.NextCpf());

        // Quando o administrador filtrar pelo Quiosque A
        var found = await SearchAsync(new { kioskId = kioskA.Id });

        // Então vê apenas as reservas do Quiosque A
        var single = Assert.Single(found);
        Assert.Equal(inA, single.Reservation.Code);
        Assert.Equal(kioskA.Id, single.KioskId);
    }

    [Fact]
    public async Task RF12_FiltraPorCpfEStatusComCpfMascaradoNaResposta()
    {
        var cpf = TestData.NextCpf();
        var code = await CreateCustomerReservationAsync(await _data.KioskAsync(), Nov20, cpf);

        var byCpf = await SearchAsync(new { cpf = TestData.FormatCpf(cpf), status = "PENDENTE_PAGAMENTO" });

        var single = Assert.Single(byCpf);
        Assert.Equal(code, single.Reservation.Code);
        Assert.Equal($"***.{cpf[3..6]}.{cpf[6..9]}-**", single.Reservation.MaskedCpf); // RNF07
        Assert.Equal("joao@exemplo.com", single.CustomerEmail);
        Assert.Empty(await SearchAsync(new { cpf = TestData.FormatCpf(cpf), status = "CONFIRMADA" }));
    }

    [Fact]
    public async Task Cenario_CriarReservaJaPagaNaAdministracao()
    {
        // Dado que o Quiosque A está livre amanhã
        var kiosk = await _data.KioskAsync();

        // Quando o administrador criar uma reserva para amanhã como "paga na administração"
        var response = await AdminCreateAsync(kiosk.Id, Tomorrow, TestData.NextCpf(), paid: true);

        // Então a reserva é criada como "Confirmada"
        // E não depende da antecedência mínima de 2 dias
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<AdminReservation>())!;
        Assert.Equal("CONFIRMADA", created.Reservation.Status);
        Assert.Equal(Tomorrow, created.Reservation.Date);
        Assert.Equal("ADMINISTRACAO", created.PaidWith); // RN14

        await using var context = _fixture.CreateContext();
        var payment = await context.Payments.SingleAsync(p => p.GatewayTransactionId == $"ADM-{created.Reservation.Code}");
        Assert.Equal((PaymentStatus.Approved, PaymentMethod.Administration), (payment.Status, payment.Method));
    }

    [Fact]
    public async Task Cenario_CriarReservaAguardandoPix()
    {
        var kiosk = await _data.KioskAsync();
        var cpf = TestData.NextCpf();

        // Quando o administrador criar uma reserva sem marcar como paga
        var response = await AdminCreateAsync(kiosk.Id, Nov20, cpf, paid: false);

        // Então a reserva é criada aguardando pagamento
        var created = (await response.Content.ReadFromJsonAsync<AdminReservation>())!;
        Assert.Equal("PENDENTE_PAGAMENTO", created.Reservation.Status);
        Assert.Null(created.PaidWith);

        // O cliente paga pelo fluxo normal, com o código e o CPF.
        var payment = await _client.PostAsJsonAsync($"/api/reservas/{created.Reservation.Code}/pagamento", new { cpf });
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);

        // E o administrador vê o mesmo Pix, para mostrar no balcão.
        var adminPayment = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{created.Reservation.Code}/pagamento", await AdminTokenAsync());
        Assert.Equal(HttpStatusCode.OK, adminPayment.StatusCode);
        Assert.Equal(
            (await payment.Content.ReadFromJsonAsync<PaymentResponse>())!.TransactionId,
            (await adminPayment.Content.ReadFromJsonAsync<PaymentResponse>())!.TransactionId);
    }

    [Fact]
    public async Task RF13_ReservaJaPagaNaoGeraPix()
    {
        var created = (await (await AdminCreateAsync((await _data.KioskAsync()).Id, Nov20, TestData.NextCpf(), paid: true))
            .Content.ReadFromJsonAsync<AdminReservation>())!;

        var response = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{created.Reservation.Code}/pagamento", await AdminTokenAsync());

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "Esta reserva não está aguardando pagamento");
    }

    [Fact]
    public async Task RN13_AdministradorNaoTemLimiteDeReservasPorCpf()
    {
        var cpf = TestData.NextCpf();

        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 3; i++)
        {
            responses.Add(await AdminCreateAsync((await _data.KioskAsync()).Id, Nov20, cpf, paid: true));
        }

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
    }

    [Fact]
    public async Task Cenario_AdministradorTambemRespeitaADataOcupada()
    {
        // Dado que o Quiosque A já tem reserva ativa em 20/11/2026
        var kioskA = await _data.KioskAsync();
        await _data.ReservationAsync(kioskA, Nov20, ReservationStatus.Confirmed);
        var other = await CreateCustomerReservationAsync(await _data.KioskAsync(), Nov21, TestData.NextCpf());

        // Quando o administrador criar ou mover outra reserva para o Quiosque A em 20/11/2026
        var created = await AdminCreateAsync(kioskA.Id, Nov20, TestData.NextCpf(), paid: true);
        var moved = await SendAsync(HttpMethod.Put, $"/api/admin/reservas/{other}", await AdminTokenAsync(), new { kioskId = kioskA.Id, date = Nov20 });

        // Então o sistema informa "Data indisponível"
        await AssertErrorAsync(created, HttpStatusCode.Conflict, "Data indisponível");
        await AssertErrorAsync(moved, HttpStatusCode.Conflict, "Data indisponível");
    }

    [Fact]
    public async Task Cenario_AlterarQuiosqueEData()
    {
        // Dado que existe uma reserva confirmada no Quiosque A em 20/11/2026
        var kioskA = await _data.KioskAsync();
        var kioskB = await _data.KioskAsync();
        var created = (await (await AdminCreateAsync(kioskA.Id, Nov20, TestData.NextCpf(), paid: true)).Content.ReadFromJsonAsync<AdminReservation>())!;

        // Quando o administrador alterar para o Quiosque B em 21/11/2026
        var response = await SendAsync(HttpMethod.Put, $"/api/admin/reservas/{created.Reservation.Code}", await AdminTokenAsync(),
            new { kioskId = kioskB.Id, date = Nov21 });

        // Então a reserva passa a ser do Quiosque B em 21/11/2026
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<AdminReservation>())!;
        Assert.Equal((kioskB.Id, kioskB.Name, Nov21), (updated.KioskId, updated.Reservation.KioskName, updated.Reservation.Date));
        Assert.Equal(created.Reservation.Amount, updated.Reservation.Amount);

        // E a data 20/11/2026 do Quiosque A fica livre
        Assert.Equal("LIVRE", await CalendarStatusAsync(kioskA.Id, Nov20));
        Assert.Equal("OCUPADO", await CalendarStatusAsync(kioskB.Id, Nov21));
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Expired)]
    public async Task RF14_ReservaInativaNaoPodeSerAlterada(ReservationStatus status)
    {
        var kiosk = await _data.KioskAsync();
        var reservation = await _data.ReservationAsync(kiosk, Nov20, status);

        var response = await SendAsync(HttpMethod.Put, $"/api/admin/reservas/{reservation.Code}", await AdminTokenAsync(),
            new { kioskId = kiosk.Id, date = Nov21 });

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "Só reservas ativas podem ser alteradas");
    }

    [Fact]
    public async Task Cenario_CancelarAQualquerMomento()
    {
        // Dado que existe uma reserva confirmada para amanhã, paga via Pix
        var cpf = TestData.NextCpf();
        var code = await CreateCustomerReservationAsync(await _data.KioskAsync(), Nov20, cpf);
        var payment = await _client.PostAsJsonAsync($"/api/reservas/{code}/pagamento", new { cpf });
        var transactionId = (await payment.Content.ReadFromJsonAsync<PaymentResponse>())!.TransactionId;
        await _client.PostAsync($"/api/dev/pagamentos/{transactionId}/aprovar", null);
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 11, 19, 15, 0, 0, TimeSpan.Zero);

        // Quando o administrador cancelar a reserva
        var response = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{code}/cancelamento", await AdminTokenAsync());

        // Então a reserva passa para "Cancelada" e o valor é reembolsado
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELADA", (await response.Content.ReadFromJsonAsync<AdminReservation>())!.Reservation.Status);
        Assert.Equal(GatewayPaymentStatus.Refunded, _fixture.Gateway.StatusOf(transactionId));
    }

    [Fact]
    public async Task RN14_CancelarReservaPagaNaAdministracaoNaoUsaOGateway()
    {
        var created = (await (await AdminCreateAsync((await _data.KioskAsync()).Id, Tomorrow, TestData.NextCpf(), paid: true))
            .Content.ReadFromJsonAsync<AdminReservation>())!;
        var transactionId = $"ADM-{created.Reservation.Code}";

        var response = await SendAsync(HttpMethod.Post, $"/api/admin/reservas/{created.Reservation.Code}/cancelamento", await AdminTokenAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(_fixture.Gateway.StatusOf(transactionId));
        await using var context = _fixture.CreateContext();
        Assert.Equal(PaymentStatus.Refunded, (await context.Payments.SingleAsync(p => p.GatewayTransactionId == transactionId)).Status);
    }

    private async Task<string> AdminTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/admin/login", new { email = PostgresFixture.AdminEmail, password = PostgresFixture.AdminPassword });
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
    }

    private async Task<HttpResponseMessage> AdminCreateAsync(Guid kioskId, DateOnly date, string cpf, bool paid) =>
        await SendAsync(HttpMethod.Post, "/api/admin/reservas", await AdminTokenAsync(), new
        {
            kioskId, date, cpf, name = "Cliente do Balcão", email = "balcao@exemplo.com", phone = "47999990000", paid,
        });

    private async Task<List<AdminReservation>> SearchAsync(object filter)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/admin/reservas/busca", await AdminTokenAsync(), filter);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<AdminReservation>>())!;
    }

    private async Task<string> CreateCustomerReservationAsync(Kiosk kiosk, DateOnly date, string cpf)
    {
        var response = await _client.PostAsJsonAsync("/api/reservas", new
        {
            kioskId = kiosk.Id, date, cpf, name = "João da Silva", email = "joao@exemplo.com", phone = "47999998888",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CodeResponse>())!.Code;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return _client.SendAsync(request);
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

    private record TokenResponse(string Token, string? Name);

    private record CodeResponse(string Code);

    private record PaymentResponse(string TransactionId);

    private record ReservationView(string Code, string KioskName, DateOnly Date, decimal Amount, string Status, string MaskedCpf);

    private record AdminReservation(ReservationView Reservation, Guid KioskId, string CustomerEmail, string CustomerPhone, string? PaidWith, bool IsActive);

    private record ErrorResponse(string Message);

    private record CalendarResponse(List<CalendarDayResponse> Days);

    private record CalendarDayResponse(DateOnly Date, string Status);
}
