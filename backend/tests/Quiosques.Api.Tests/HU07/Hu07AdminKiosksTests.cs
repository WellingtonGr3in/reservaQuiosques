using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU07;

// HU07 — Administrar quiosques (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu07AdminKiosksTests
{
    private static readonly DateOnly Nov20 = new(2026, 11, 20);

    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu07AdminKiosksTests(PostgresFixture fixture)
    {
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_CadastrarQuiosque()
    {
        // Quando o administrador cadastrar o "Quiosque G" com capacidade, valor, fotos, itens e regras de uso
        var name = $"Quiosque G {Guid.NewGuid():N}";
        var response = await SendAsync(HttpMethod.Post, "/api/admin/quiosques", NewKiosk(name, 150m));

        // Então o Quiosque G aparece na lista de quiosques para os clientes
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<AdminKiosk>())!;
        var publicList = await _client.GetFromJsonAsync<List<PublicKiosk>>("/api/quiosques");
        var shown = Assert.Single(publicList!, k => k.Id == created.Id);
        Assert.Equal((name, 40, 150m), (shown.Name, shown.Capacity, shown.DailyRate));

        var details = await _client.GetFromJsonAsync<PublicKioskDetails>($"/api/quiosques/{created.Id}");
        Assert.Equal(["Churrasqueira", "Pia"], details!.Items.Order());
        Assert.Equal(["/imagens/g1.jpg", "/imagens/g2.jpg"], details.Photos);
        Assert.Equal("Sem som alto.", details.UsageRules);
    }

    [Fact]
    public async Task Cenario_EditarQuiosque()
    {
        var created = await CreateKioskAsync(150m);
        var existing = await _data.ReservationAsync(created.Id, 150m, Nov20, ReservationStatus.Confirmed);

        // Quando o administrador alterar o valor da diária do Quiosque G para R$ 180,00
        var response = await SendAsync(HttpMethod.Put, $"/api/admin/quiosques/{created.Id}", NewKiosk(created.Name, 180m) with { Items = ["Só grelha"] });

        // Então novas reservas do Quiosque G custam R$ 180,00
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Só grelha"], (await response.Content.ReadFromJsonAsync<AdminKiosk>())!.Items);
        var reservation = await _client.PostAsJsonAsync("/api/reservas", new
        {
            kioskId = created.Id, date = new DateOnly(2026, 11, 21), cpf = TestData.NextCpf(),
            name = "Maria", email = "maria@exemplo.com", phone = "47999998888",
        });
        Assert.Equal(180m, (await reservation.Content.ReadFromJsonAsync<ReservationAmount>())!.Amount);

        // E reservas já feitas mantêm o valor do momento da reserva
        Assert.Equal(150m, existing.Amount);
    }

    [Fact]
    public async Task Cenario_DesativarQuiosque()
    {
        var created = await CreateKioskAsync(150m);

        // Quando o administrador desativar o Quiosque G
        var response = await SendAsync(HttpMethod.Put, $"/api/admin/quiosques/{created.Id}", NewKiosk(created.Name, 150m) with { IsActive = false });

        // Então o Quiosque G deixa de aparecer para os clientes
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(await _client.GetFromJsonAsync<List<PublicKiosk>>("/api/quiosques") ?? [], k => k.Id == created.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/quiosques/{created.Id}")).StatusCode);

        // O administrador continua vendo e pode reativar.
        var adminList = await (await SendAsync(HttpMethod.Get, "/api/admin/quiosques")).Content.ReadFromJsonAsync<List<AdminKiosk>>();
        Assert.False(adminList!.Single(k => k.Id == created.Id).IsActive);
    }

    [Fact]
    public async Task Cenario_ExcluirQuiosqueComReservas()
    {
        // Dado que o Quiosque A tem reservas
        var created = await CreateKioskAsync(150m);
        await _data.ReservationAsync(created.Id, 150m, Nov20, ReservationStatus.Cancelled);

        // Quando o administrador tentar excluir o Quiosque A
        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/quiosques/{created.Id}");

        // Então o sistema informa que o quiosque tem reservas e deve ser desativado
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Este quiosque tem reservas e não pode ser excluído. Desative-o.",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task Cenario_ExcluirQuiosqueSemReservas()
    {
        // Dado que o Quiosque G nunca teve reservas
        var created = await CreateKioskAsync(150m);

        // Quando o administrador excluir o Quiosque G
        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/quiosques/{created.Id}");

        // Então o Quiosque G deixa de existir
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/admin/quiosques/{created.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("", 30, 150, "Nome é obrigatório e deve ter até 100 caracteres")]
    [InlineData("Quiosque X", 0, 150, "A capacidade deve ser entre 1 e 1000 pessoas")]
    [InlineData("Quiosque X", 30, 0, "Valor da diária inválido")]
    public async Task RF16_DadosInvalidos(string name, int capacity, decimal dailyRate, string message)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/admin/quiosques", NewKiosk(name, dailyRate) with { Capacity = capacity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(message, (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task RF16_EnviarFotoDoQuiosque()
    {
        // PNG de 1x1 pixel.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        var response = await UploadAsync(png, "foto.png");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var url = (await response.Content.ReadFromJsonAsync<ImageResponse>())!.Url;
        Assert.Matches("^/uploads/[0-9a-f]{32}\\.png$", url);

        // A imagem fica disponível no endereço devolvido, para usar como capa ou na galeria.
        var image = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal(png, await image.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task RF16_ArquivoQueNaoEImagemERecusado()
    {
        // O nome diz .jpg, mas o conteúdo é texto: o tipo é conferido pelos bytes do arquivo.
        var response = await UploadAsync("isto não é uma imagem"u8.ToArray(), "foto.jpg");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Envie uma imagem JPG, PNG ou WEBP de até 5 MB", (await response.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task RF16_EnviarFotoExigeLoginDeAdministrador()
    {
        var form = new MultipartFormDataContent { { new ByteArrayContent([0xFF, 0xD8, 0xFF]), "arquivo", "foto.jpg" } };

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsync("/api/admin/imagens", form)).StatusCode);
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName)
    {
        var login = await _client.PostAsJsonAsync("/api/admin/login", new { email = PostgresFixture.AdminEmail, password = PostgresFixture.AdminPassword });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/imagens")
        {
            Content = new MultipartFormDataContent { { new ByteArrayContent(bytes), "arquivo", fileName } },
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private record ImageResponse(string Url);

    private static KioskRequest NewKiosk(string name, decimal dailyRate) => new(
        name, "Quiosque novo do condomínio.", 40, dailyRate, "/imagens/g1.jpg", "Sem som alto.", true,
        ["Churrasqueira", "Pia"], ["/imagens/g1.jpg", "/imagens/g2.jpg"]);

    private async Task<AdminKiosk> CreateKioskAsync(decimal dailyRate)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/admin/quiosques", NewKiosk($"Quiosque G {Guid.NewGuid():N}", dailyRate));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AdminKiosk>())!;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var login = await _client.PostAsJsonAsync("/api/admin/login", new { email = PostgresFixture.AdminEmail, password = PostgresFixture.AdminPassword });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private record KioskRequest(
        string Name, string Description, int Capacity, decimal DailyRate, string PhotoUrl, string UsageRules,
        bool IsActive, List<string> Items, List<string> Photos);

    private record AdminKiosk(Guid Id, string Name, decimal DailyRate, List<string> Items, bool IsActive, bool HasReservations);

    private record PublicKiosk(Guid Id, string Name, int Capacity, decimal DailyRate);

    private record PublicKioskDetails(List<string> Items, List<string> Photos, string UsageRules);

    private record ReservationAmount(decimal Amount);

    private record TokenResponse(string Token);

    private record ErrorResponse(string Message);
}
