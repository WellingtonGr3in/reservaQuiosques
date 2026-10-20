using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU05;

// HU05 — Criar conta e ver histórico (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu05AccountTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);
    private const string Password = "senha-segura-123";

    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu05AccountTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_ContaHerdaReservasAnteriores()
    {
        // Dado que fiz 2 reservas sem conta com meu CPF
        var cpf = TestData.NextCpf();
        var first = await CreateReservationAsync(cpf, NewEmail());
        var second = await CreateReservationAsync(cpf, NewEmail());

        // Quando eu criar uma conta com esse CPF (o e-mail de login pode ser qualquer um)
        var response = await CreateAccountAsync(cpf, NewEmail());

        // Então as 2 reservas aparecem em "Minhas reservas"
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        var mine = await GetMyReservationsAsync(token);
        Assert.Equal(new[] { first, second }.Order(), mine.Reservations.Select(r => r.Code).Order());
        Assert.Equal($"***.{cpf[3..6]}.{cpf[6..9]}-**", mine.MaskedCpf); // RNF07
    }

    [Fact]
    public async Task Cenario_CpfSemReservas()
    {
        // Dado que meu CPF ainda não fez nenhuma reserva
        // Quando eu tentar criar uma conta com esse CPF
        var response = await CreateAccountAsync(TestData.NextCpf(), NewEmail());

        // Então o sistema informa "Não encontramos reservas com este CPF"
        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Não encontramos reservas com este CPF");
    }

    [Fact]
    public async Task RN09_CpfInvalidoDaAMesmaMensagem()
    {
        var response = await CreateAccountAsync("111.111.111-11", NewEmail());

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Não encontramos reservas com este CPF");
    }

    [Fact]
    public async Task Cenario_CpfQueJaTemConta()
    {
        // Dado que meu CPF já possui conta
        var (cpf, email, _) = await CreateAccountWithReservationAsync();

        // Quando eu tentar criar outra conta
        var response = await CreateAccountAsync(cpf, email);

        // Então o sistema informa "Este CPF já possui conta. Faça login."
        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Este CPF já possui conta. Faça login.");
    }

    [Fact]
    public async Task RN09_CriacoesSimultaneasParaOMesmoCpfGeramUmaSoConta()
    {
        var cpf = TestData.NextCpf();
        var email = NewEmail();
        await CreateReservationAsync(cpf, email);

        var responses = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => CreateAccountAsync(cpf, email)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task Cenario_LoginComDadosCorretos()
    {
        // Dado que tenho uma conta
        var (_, email, code) = await CreateAccountWithReservationAsync();

        // Quando eu entrar com meu e-mail e senha
        var response = await LoginAsync(email.ToUpperInvariant(), Password);

        // Então acesso "Minhas reservas"
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Token;
        Assert.Contains((await GetMyReservationsAsync(token)).Reservations, r => r.Code == code);
    }

    [Fact]
    public async Task Cenario_LoginComSenhaErrada()
    {
        var (_, email, _) = await CreateAccountWithReservationAsync();

        // Quando eu entrar com uma senha errada
        var response = await LoginAsync(email, "senha-errada-123");

        // Então o sistema informa "E-mail ou senha inválidos"
        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "E-mail ou senha inválidos");
    }

    [Fact]
    public async Task RF09_LoginComEmailInexistenteDaAMesmaMensagem()
    {
        var response = await LoginAsync(NewEmail(), Password);

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "E-mail ou senha inválidos");
    }

    [Fact]
    public async Task RNF04_SenhaGuardadaComBcryptCusto12ETokenDe60Minutos()
    {
        var (_, email, _) = await CreateAccountWithReservationAsync();

        await using var context = _fixture.CreateContext();
        var hash = (await context.Accounts.SingleAsync(a => a.LoginEmail == email)).PasswordHash;
        Assert.Matches(@"^\$2[aby]\$12\$", hash);
        Assert.DoesNotContain(Password, hash);

        var login = (await (await LoginAsync(email, Password)).Content.ReadFromJsonAsync<TokenResponse>())!;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        Assert.Equal(TimeSpan.FromMinutes(60), jwt.ValidTo - jwt.ValidFrom);
    }

    [Fact]
    public async Task RF10_MinhasReservasExigeLogin()
    {
        var withoutToken = await _client.GetAsync("/api/minhas-reservas");

        var invalidToken = new HttpRequestMessage(HttpMethod.Get, "/api/minhas-reservas");
        invalidToken.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        Assert.Equal(HttpStatusCode.Unauthorized, withoutToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(invalidToken)).StatusCode);
    }

    [Fact]
    public async Task RF10_MostraSoAsReservasDoProprioCliente()
    {
        var (_, email, code) = await CreateAccountWithReservationAsync();
        var othersCode = await CreateReservationAsync(TestData.NextCpf(), NewEmail());

        var mine = await GetMyReservationsAsync(await TokenAsync(email));

        Assert.Equal([code], mine.Reservations.Select(r => r.Code));
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAuthorizedAsync(HttpMethod.Get, $"/api/minhas-reservas/{othersCode}", await TokenAsync(email))).StatusCode);
    }

    [Fact]
    public async Task Realizada_ReservaConfirmadaComDataPassadaApareceComoRealizada()
    {
        var (cpf, email, code) = await CreateAccountWithReservationAsync();

        var pastCode = TestData.NextCode();
        await using (var context = _fixture.CreateContext())
        {
            var customer = await context.Customers.SingleAsync(c => c.Cpf == cpf);
            var kiosk = await _data.KioskAsync();
            context.Add(new Reservation(pastCode, customer.Id, kiosk.Id, new DateOnly(2026, 9, 5), 150m,
                ReservationStatus.Confirmed, TestData.Now, TestData.Now));
            await context.SaveChangesAsync();
        }

        var mine = await GetMyReservationsAsync(await TokenAsync(email));

        Assert.Equal("REALIZADA", mine.Reservations.Single(r => r.Code == pastCode).Status);
        Assert.Equal("PENDENTE_PAGAMENTO", mine.Reservations.Single(r => r.Code == code).Status);
    }

    [Fact]
    public async Task RN07_CancelamentoComLogin()
    {
        var (_, email, code) = await CreateAccountWithReservationAsync();
        var token = await TokenAsync(email);

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/minhas-reservas/{code}/cancelamento", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CANCELADA", (await response.Content.ReadFromJsonAsync<ReservationItem>())!.Status);
    }

    [Fact]
    public async Task RN07_CancelamentoComLoginRespeitaOPrazoDe48h()
    {
        var (cpf, email, code) = await CreateAccountWithReservationAsync();
        var payment = await _client.PostAsJsonAsync($"/api/reservas/{code}/pagamento", new { cpf });
        var transactionId = (await payment.Content.ReadFromJsonAsync<PaymentResponse>())!.TransactionId;
        await _client.PostAsync($"/api/dev/pagamentos/{transactionId}/aprovar", null);
        var token = await TokenAsync(email);

        // Reserva confirmada para 20/11; agora é 19/11, menos de 48h antes.
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 11, 19, 15, 0, 0, TimeSpan.Zero);
        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/minhas-reservas/{code}/cancelamento", token);

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "O cancelamento só é permitido até 48h antes da data reservada");
    }

    [Fact]
    public async Task RN07_CancelamentoComLoginDeReservaDeOutroClienteNaoEncontra()
    {
        var (_, email, _) = await CreateAccountWithReservationAsync();
        var othersCode = await CreateReservationAsync(TestData.NextCpf(), NewEmail());

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/minhas-reservas/{othersCode}/cancelamento", await TokenAsync(email));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Reserva não encontrada");
    }

    [Fact]
    public async Task Cenario_ReservarLogadoComDadosJaPreenchidos()
    {
        // Dado que estou logado na minha conta
        var (cpf, email, _) = await CreateAccountWithReservationAsync();
        var token = await TokenAsync(email);

        // Quando eu abrir a tela de dados de uma nova reserva
        var profile = await (await SendAuthorizedAsync(HttpMethod.Get, "/api/minha-conta", token)).Content.ReadFromJsonAsync<ProfileResponse>();

        // Então meu nome, e-mail e telefone já vêm preenchidos e o CPF aparece mascarado
        Assert.Equal(("João da Silva", email, "47999998888"), (profile!.Name, profile.Email, profile.Phone));
        Assert.Equal($"***.{cpf[3..6]}.{cpf[6..9]}-**", profile.MaskedCpf); // RNF07

        // E a reserva é feita com o CPF da conta, sem informá-lo de novo
        var kiosk = await _data.KioskAsync();
        var created = await SendAuthorizedAsync(HttpMethod.Post, "/api/minhas-reservas", token,
            new { kioskId = kiosk.Id, date = new DateOnly(2026, 11, 21), profile.Name, profile.Email, profile.Phone });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var code = (await created.Content.ReadFromJsonAsync<CodeResponse>())!.Code;
        Assert.Contains((await GetMyReservationsAsync(token)).Reservations, r => r.Code == code);

        // E o pagamento também usa o login
        var payment = await SendAuthorizedAsync(HttpMethod.Post, $"/api/minhas-reservas/{code}/pagamento", token);
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
    }

    [Fact]
    public async Task RN08_LimiteDeReservasTambemValeComLogin()
    {
        var (_, email, _) = await CreateAccountWithReservationAsync(); // 1ª reserva ativa
        var token = await TokenAsync(email);
        object NewReservation(Guid kioskId) => new { kioskId, date = Nov20, name = "João da Silva", email, phone = "47999998888" };

        var second = await SendAuthorizedAsync(HttpMethod.Post, "/api/minhas-reservas", token, NewReservation((await _data.KioskAsync()).Id));
        var third = await SendAuthorizedAsync(HttpMethod.Post, "/api/minhas-reservas", token, NewReservation((await _data.KioskAsync()).Id));

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        await AssertErrorAsync(third, HttpStatusCode.UnprocessableEntity, "Limite de 2 reservas ativas por CPF atingido");
    }

    [Fact]
    public async Task RF05_PagamentoLogadoDeReservaDeOutroClienteNaoEncontra()
    {
        var (_, email, _) = await CreateAccountWithReservationAsync();
        var othersCode = await CreateReservationAsync(TestData.NextCpf(), NewEmail());

        var response = await SendAuthorizedAsync(HttpMethod.Post, $"/api/minhas-reservas/{othersCode}/pagamento", await TokenAsync(email));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "Reserva não encontrada");
    }

    [Fact]
    public async Task EmailDeLoginJaUsadoPorOutroCpf_RetornaConflito()
    {
        // Duas pessoas da mesma família reservaram com o mesmo e-mail; só uma conta pode usá-lo para entrar.
        var (_, email, _) = await CreateAccountWithReservationAsync();
        var otherCpf = TestData.NextCpf();
        await CreateReservationAsync(otherCpf, email);

        var response = await CreateAccountAsync(otherCpf, email);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Este e-mail já está em uso por outra conta");
    }

    [Theory]
    [InlineData("curta")]
    [InlineData("")]
    public async Task SenhaInvalida_RetornaRequisicaoInvalida(string password)
    {
        var cpf = TestData.NextCpf();
        var email = NewEmail();
        await CreateReservationAsync(cpf, email);

        var response = await _client.PostAsJsonAsync("/api/contas", new { cpf, email, password });

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "A senha deve ter entre 8 e 72 caracteres");
    }

    private async Task<string> CreateReservationAsync(string cpf, string email)
    {
        var kiosk = await _data.KioskAsync();
        var response = await _client.PostAsJsonAsync("/api/reservas", new
        {
            kioskId = kiosk.Id, date = Nov20, cpf, name = "João da Silva", email, phone = "47999998888",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CodeResponse>())!.Code;
    }

    private Task<HttpResponseMessage> CreateAccountAsync(string cpf, string email) =>
        _client.PostAsJsonAsync("/api/contas", new { cpf, email, password = Password });

    private async Task<(string Cpf, string Email, string Code)> CreateAccountWithReservationAsync()
    {
        var cpf = TestData.NextCpf();
        var email = NewEmail();
        var code = await CreateReservationAsync(cpf, email);
        Assert.Equal(HttpStatusCode.Created, (await CreateAccountAsync(cpf, email)).StatusCode);
        return (cpf, email, code);
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/login", new { email, password });

    private async Task<string> TokenAsync(string email) =>
        (await (await LoginAsync(email, Password)).Content.ReadFromJsonAsync<TokenResponse>())!.Token;

    private Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return _client.SendAsync(request);
    }

    private async Task<MyReservationsResponse> GetMyReservationsAsync(string token)
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "/api/minhas-reservas", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MyReservationsResponse>())!;
    }

    private static string NewEmail() => $"cliente-{Guid.NewGuid():N}@exemplo.com";

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(message, (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    private record CodeResponse(string Code);

    private record ProfileResponse(string Name, string Email, string Phone, string MaskedCpf);

    private record PaymentResponse(string TransactionId);

    private record TokenResponse(string Token, DateTimeOffset ExpiresAt);

    private record MyReservationsResponse(string HolderName, string MaskedCpf, List<ReservationItem> Reservations);

    private record ReservationItem(string Code, string Status, DateOnly Date, bool CanCancel);

    private record ErrorResponse(string Message);
}
