using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU02;

// HU02 — Reservar sem criar conta (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu02CreateReservationTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);

    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu02CreateReservationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_ReservaCriadaComSucesso()
    {
        // Dado que o Quiosque A está livre em 20/11/2026
        var kiosk = await _data.KioskAsync();

        // Quando eu informar CPF, nome, e-mail e telefone válidos
        // E solicitar a reserva para 20/11/2026
        var response = await PostAsync(Request(kiosk.Id, Nov20, TestData.FormatCpf(TestData.NextCpf())));

        // Então a reserva é criada aguardando pagamento
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reservation = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        Assert.Equal("PENDENTE_PAGAMENTO", reservation.Status);
        Assert.Matches("^QSK-[A-HJ-NP-Z2-9]{6}$", reservation.Code);
        Assert.Equal(kiosk.DailyRate, reservation.Amount);

        // E a data fica bloqueada por 15 minutos
        Assert.Equal(TestData.Now.AddMinutes(15), reservation.ExpiresAt);
        Assert.Equal("OCUPADO", await CalendarStatusAsync(kiosk.Id, Nov20));
        _fixture.Clock.UtcNow = TestData.Now.AddMinutes(15);
        Assert.Equal("LIVRE", await CalendarStatusAsync(kiosk.Id, Nov20));
    }

    [Fact]
    public async Task Cenario_DataJaReservada()
    {
        // Dado que o Quiosque A já tem reserva ativa em 20/11/2026
        var kiosk = await _data.KioskAsync();
        await _data.ReservationAsync(kiosk, Nov20, ReservationStatus.Confirmed);

        // Quando eu tentar reservar a mesma data
        var response = await PostAsync(Request(kiosk.Id, Nov20, TestData.NextCpf()));

        // Então o sistema informa "Data indisponível"
        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Data indisponível");
    }

    [Fact]
    public async Task Cenario_DuasPessoasReservandoAoMesmoTempo()
    {
        // Dado que o Quiosque A está livre em 20/11/2026
        var kiosk = await _data.KioskAsync();

        // Quando dois clientes confirmarem a reserva no mesmo instante
        var responses = await Task.WhenAll(
            PostAsync(Request(kiosk.Id, Nov20, TestData.NextCpf())),
            PostAsync(Request(kiosk.Id, Nov20, TestData.NextCpf())));

        // Então apenas uma reserva é criada
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(1, await CountReservationsAsync(kiosk.Id));

        // E o outro cliente recebe "Data indisponível"
        await AssertErrorAsync(responses.Single(r => r.StatusCode != HttpStatusCode.Created), HttpStatusCode.Conflict, "Data indisponível");
    }

    [Fact]
    public async Task RNF02_CemRequisicoesSimultaneasCriamExatamenteUmaReserva()
    {
        var kiosk = await _data.KioskAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 100).Select(_ => PostAsync(Request(kiosk.Id, Nov20, TestData.NextCpf()))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal(1, await CountReservationsAsync(kiosk.Id));
    }

    [Theory]
    [InlineData(2026, 10, 10)] // 1 dia de antecedência
    [InlineData(2027, 1, 8)]   // 91 dias de antecedência
    public async Task Cenario_DataForaDaJanelaDeAntecedencia(int year, int month, int day)
    {
        // Dado que hoje é 09/10/2026
        var kiosk = await _data.KioskAsync();

        // Quando eu tentar reservar o Quiosque A para 10/10/2026 ou para 08/01/2027
        var response = await PostAsync(Request(kiosk.Id, new DateOnly(year, month, day), TestData.NextCpf()));

        // Então o sistema informa que a reserva deve ser feita com 2 a 90 dias de antecedência
        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "A reserva deve ser feita com 2 a 90 dias de antecedência");
    }

    [Theory]
    [InlineData(2026, 10, 11)] // 2 dias
    [InlineData(2027, 1, 7)]   // 90 dias
    public async Task RN03_LimitesDaJanelaSaoAceitos(int year, int month, int day)
    {
        var kiosk = await _data.KioskAsync();

        var response = await PostAsync(Request(kiosk.Id, new DateOnly(year, month, day), TestData.NextCpf()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Cenario_CpfInvalido()
    {
        var kiosk = await _data.KioskAsync();

        // Quando eu informar o CPF "111.111.111-11"
        var response = await PostAsync(Request(kiosk.Id, Nov20, "111.111.111-11"));

        // Então o sistema informa "CPF inválido"
        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "CPF inválido");
    }

    [Fact]
    public async Task Cenario_LimiteDeReservasPorCpf()
    {
        // Dado que meu CPF já tem 2 reservas ativas
        var cpf = TestData.NextCpf();
        var first = await _data.KioskAsync();
        var second = await _data.KioskAsync();
        var third = await _data.KioskAsync();
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(Request(first.Id, Nov20, cpf))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostAsync(Request(second.Id, Nov20, cpf))).StatusCode);

        // Quando eu tentar fazer outra reserva
        var response = await PostAsync(Request(third.Id, Nov20, cpf));

        // Então o sistema informa que o limite foi atingido
        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "Limite de 2 reservas ativas por CPF atingido");
    }

    [Fact]
    public async Task RN08_ReservasSimultaneasDoMesmoCpfRespeitamOLimite()
    {
        var cpf = TestData.NextCpf();
        var kiosks = new[] { await _data.KioskAsync(), await _data.KioskAsync(), await _data.KioskAsync(), await _data.KioskAsync() };

        var responses = await Task.WhenAll(kiosks.Select(k => PostAsync(Request(k.Id, Nov20, cpf))));

        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
    }

    [Theory]
    [InlineData(ReservationStatus.Expired)]
    [InlineData(ReservationStatus.Cancelled)]
    public async Task RN08_ReservasInativasNaoContamNoLimite(ReservationStatus inactive)
    {
        var cpf = TestData.NextCpf();
        var customer = await _data.AddAsync(new Customer(cpf, "Maria", "maria@exemplo.com", "47999998888", TestData.Now));
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var kiosk = await _data.KioskAsync();
            await _data.AddAsync(new Reservation(TestData.NextCode(), customer.Id, kiosk.Id, Nov20, 150m, inactive, TestData.Now, TestData.Now));
        }

        var response = await PostAsync(Request((await _data.KioskAsync()).Id, Nov20, cpf));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task RN08_ReservasConfirmadasDeDatasPassadasNaoContamNoLimite()
    {
        var cpf = TestData.NextCpf();
        var customer = await _data.AddAsync(new Customer(cpf, "Maria", "maria@exemplo.com", "47999998888", TestData.Now));
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var kiosk = await _data.KioskAsync();
            await _data.AddAsync(new Reservation(
                TestData.NextCode(), customer.Id, kiosk.Id, new DateOnly(2026, 9, 5), 150m, ReservationStatus.Confirmed, TestData.Now, TestData.Now));
        }

        var response = await PostAsync(Request((await _data.KioskAsync()).Id, Nov20, cpf));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Cenario_DadosDeContatoAtualizados()
    {
        // Dado que meu CPF já fez uma reserva com o e-mail "antigo@exemplo.com"
        var cpf = TestData.NextCpf();
        await PostAsync(Request((await _data.KioskAsync()).Id, Nov20, cpf, email: "antigo@exemplo.com"));

        // Quando eu fizer nova reserva com o e-mail "novo@exemplo.com"
        var response = await PostAsync(Request((await _data.KioskAsync()).Id, Nov20, cpf, email: "novo@exemplo.com"));

        // Então o cadastro do cliente passa a ter o e-mail "novo@exemplo.com"
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var context = _fixture.CreateContext();
        var customer = await context.Customers.SingleAsync(c => c.Cpf == cpf);
        Assert.Equal("novo@exemplo.com", customer.Email);
    }

    [Fact]
    public async Task RN12_ReservaRecusadaNaoAlteraOCadastro()
    {
        var cpf = TestData.NextCpf();
        await PostAsync(Request((await _data.KioskAsync()).Id, Nov20, cpf, email: "antigo@exemplo.com"));
        var occupied = await _data.KioskAsync();
        await _data.ReservationAsync(occupied, Nov20, ReservationStatus.Confirmed);

        var response = await PostAsync(Request(occupied.Id, Nov20, cpf, email: "novo@exemplo.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var context = _fixture.CreateContext();
        Assert.Equal("antigo@exemplo.com", (await context.Customers.SingleAsync(c => c.Cpf == cpf)).Email);
    }

    [Fact]
    public async Task ADR001_ReservaPendenteVencidaEExpiradaAoReservarAMesmaData()
    {
        var kiosk = await _data.KioskAsync();
        var overdue = await _data.ReservationAsync(
            kiosk, Nov20, ReservationStatus.PendingPayment, expiresAt: TestData.Now.AddSeconds(-1));

        var response = await PostAsync(Request(kiosk.Id, Nov20, TestData.NextCpf()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var context = _fixture.CreateContext();
        Assert.Equal(ReservationStatus.Expired, (await context.Reservations.SingleAsync(r => r.Id == overdue.Id)).Status);
    }

    [Fact]
    public async Task QuiosqueInativoOuInexistente_RetornaNaoEncontrado()
    {
        var inactive = await _data.KioskAsync(isActive: false);

        await AssertErrorAsync(await PostAsync(Request(inactive.Id, Nov20, TestData.NextCpf())), HttpStatusCode.NotFound, "Quiosque não encontrado");
        await AssertErrorAsync(await PostAsync(Request(Guid.NewGuid(), Nov20, TestData.NextCpf())), HttpStatusCode.NotFound, "Quiosque não encontrado");
    }

    [Theory]
    [InlineData("", "joao@exemplo.com", "47999998888", "Nome inválido")]
    [InlineData("João", "joao.exemplo.com", "47999998888", "E-mail inválido")]
    [InlineData("João", "joao@exemplo.com", "9999", "Telefone inválido")]
    public async Task DadosDeContatoInvalidos_RetornamRequisicaoInvalida(string name, string email, string phone, string message)
    {
        var kiosk = await _data.KioskAsync();

        var response = await PostAsync(new { kioskId = kiosk.Id, date = Nov20, cpf = TestData.NextCpf(), name, email, phone });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, message);
    }

    [Theory]
    [InlineData("{ isto não é json")]
    [InlineData("""{ "kioskId": "nao-e-guid", "date": "2026-11-20" }""")]
    [InlineData("""{ "kioskId": "0199f5a0-0000-7000-8000-00000000000a", "date": "20/11/2026" }""")]
    public async Task CorpoMalformado_RetornaRequisicaoInvalida(string body)
    {
        var response = await _client.PostAsync("/api/reservas", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Requisição inválida");
    }

    private static object Request(Guid kioskId, DateOnly date, string cpf, string email = "joao@exemplo.com") =>
        new { kioskId, date, cpf, name = "João da Silva", email, phone = "(47) 99999-8888" };

    private Task<HttpResponseMessage> PostAsync(object request) => _client.PostAsJsonAsync("/api/reservas", request);

    private async Task<string> CalendarStatusAsync(Guid kioskId, DateOnly date)
    {
        var calendar = await _client.GetFromJsonAsync<CalendarResponse>($"/api/quiosques/{kioskId}/calendario?ano={date.Year}&mes={date.Month}");
        return calendar!.Days.Single(d => d.Date == date).Status;
    }

    private async Task<int> CountReservationsAsync(Guid kioskId)
    {
        await using var context = _fixture.CreateContext();
        return await context.Reservations.CountAsync(r => r.KioskId == kioskId);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(message, error!.Message);
    }

    private record ReservationResponse(string Code, Guid KioskId, DateOnly Date, decimal Amount, string Status, DateTimeOffset ExpiresAt);

    private record ErrorResponse(string Message);

    private record CalendarResponse(List<CalendarDayResponse> Days);

    private record CalendarDayResponse(DateOnly Date, string Status);
}
