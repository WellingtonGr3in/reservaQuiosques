using System.Net;
using System.Net.Http.Json;
using Quiosques.Api.Tests.Infrastructure;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Tests.HU01;

// HU01 — Ver quiosques e disponibilidade (docs/01-requisitos-funcionais.md).
[Collection(PostgresCollection.Name)]
public class Hu01KioskAvailabilityTests
{
    private readonly PostgresFixture _fixture;
    private readonly TestData _data;
    private readonly HttpClient _client;

    public Hu01KioskAvailabilityTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _data = new TestData(fixture);
        _client = fixture.Api.CreateClient();
        fixture.Clock.UtcNow = TestData.Now; // hoje é 09/10/2026 em Brasília
    }

    [Fact]
    public async Task Cenario_ListarApenasQuiosquesAtivos()
    {
        // Dado que existem os Quiosques A e B ativos e o Quiosque C inativo
        var active = await _data.KioskAsync(isActive: true);
        var otherActive = await _data.KioskAsync(isActive: true);
        var inactive = await _data.KioskAsync(isActive: false);

        // Quando eu abrir a lista de quiosques
        var kiosks = await _client.GetFromJsonAsync<List<KioskSummaryResponse>>("/api/quiosques");

        // Então vejo os Quiosques A e B com nome, foto, capacidade e valor da diária
        var shown = Assert.Single(kiosks!, k => k.Id == active.Id);
        Assert.Equal(active.Name, shown.Name);
        Assert.Equal(active.PhotoUrl, shown.PhotoUrl);
        Assert.Equal(active.Capacity, shown.Capacity);
        Assert.Equal(active.DailyRate, shown.DailyRate);
        Assert.Contains(kiosks!, k => k.Id == otherActive.Id);

        // E não vejo o Quiosque C
        Assert.DoesNotContain(kiosks!, k => k.Id == inactive.Id);
    }

    [Fact]
    public async Task Cenario_VerDetalhesDoQuiosque()
    {
        var kiosk = await _data.KioskAsync();
        await _data.AddAsync(new KioskItem(kiosk.Id, "Grelha e espetos"));
        await _data.AddAsync(new KioskItem(kiosk.Id, "Pia com água tratada"));
        await _data.AddAsync(new KioskPhoto(kiosk.Id, "/imagens/2.jpg", 2));
        await _data.AddAsync(new KioskPhoto(kiosk.Id, "/imagens/1.jpg", 1));

        // Quando eu abrir o Quiosque A
        var details = await _client.GetFromJsonAsync<KioskDetailsResponse>($"/api/quiosques/{kiosk.Id}");

        // Então vejo a descrição, os itens inclusos, as fotos e as regras de uso
        Assert.Equal(kiosk.Description, details!.Description);
        Assert.Equal(["Grelha e espetos", "Pia com água tratada"], details.Items.Order());
        Assert.Equal(["/imagens/1.jpg", "/imagens/2.jpg"], details.Photos);
        Assert.Equal(kiosk.UsageRules, details.UsageRules);
    }

    [Fact]
    public async Task Cenario_VerDatasLivresEOcupadas()
    {
        // Dado que o Quiosque A tem uma reserva confirmada em 21/11/2026
        var kiosk = await _data.KioskAsync();
        await _data.ReservationAsync(kiosk, new DateOnly(2026, 11, 21), ReservationStatus.Confirmed);

        // Quando eu abrir o calendário de novembro do Quiosque A
        var calendar = await GetCalendarAsync(kiosk.Id, 2026, 11);

        // Então o dia 21 aparece como "Ocupado"
        Assert.Equal("OCUPADO", StatusOf(calendar, 2026, 11, 21));

        // E os demais dias dentro da janela permitida aparecem como "Livre"
        Assert.Equal(30, calendar.Days.Count);
        Assert.All(calendar.Days.Where(d => d.Date.Day != 21), d => Assert.Equal("LIVRE", d.Status));
    }

    [Fact]
    public async Task Cenario_DatasForaDaJanelaDeAntecedencia()
    {
        // Dado que hoje é 09/10/2026
        var kiosk = await _data.KioskAsync();

        // Quando eu abrir o calendário do Quiosque A
        var october = await GetCalendarAsync(kiosk.Id, 2026, 10);
        var january = await GetCalendarAsync(kiosk.Id, 2027, 1);

        // Então os dias 09/10 e 10/10 aparecem como "Indisponível"
        Assert.Equal("INDISPONIVEL", StatusOf(october, 2026, 10, 9));
        Assert.Equal("INDISPONIVEL", StatusOf(october, 2026, 10, 10));
        // E o dia 11/10 aparece como "Livre"
        Assert.Equal("LIVRE", StatusOf(october, 2026, 10, 11));
        // E o dia 07/01/2027 aparece como "Livre"
        Assert.Equal("LIVRE", StatusOf(january, 2027, 1, 7));
        // E o dia 08/01/2027 aparece como "Indisponível"
        Assert.Equal("INDISPONIVEL", StatusOf(january, 2027, 1, 8));
    }

    [Fact]
    public async Task Cenario_ReservaPendenteComPrazoVencidoNaoOcupaAData()
    {
        // Dado que o Quiosque A tem uma reserva aguardando pagamento em 20/11/2026
        // E o prazo de 15 minutos dessa reserva já terminou
        var kiosk = await _data.KioskAsync();
        await _data.ReservationAsync(
            kiosk, new DateOnly(2026, 11, 20), ReservationStatus.PendingPayment, expiresAt: TestData.Now.AddSeconds(-1));

        // Quando eu abrir o calendário de novembro do Quiosque A
        var calendar = await GetCalendarAsync(kiosk.Id, 2026, 11);

        // Então o dia 20 aparece como "Livre"
        Assert.Equal("LIVRE", StatusOf(calendar, 2026, 11, 20));
    }

    [Fact]
    public async Task RN05_ReservaPendenteDentroDoPrazoOcupaAData()
    {
        var kiosk = await _data.KioskAsync();
        await _data.ReservationAsync(
            kiosk, new DateOnly(2026, 11, 20), ReservationStatus.PendingPayment, expiresAt: TestData.Now.AddMinutes(10));

        var calendar = await GetCalendarAsync(kiosk.Id, 2026, 11);

        Assert.Equal("OCUPADO", StatusOf(calendar, 2026, 11, 20));
    }

    [Theory]
    [InlineData(ReservationStatus.Expired)]
    [InlineData(ReservationStatus.Cancelled)]
    public async Task RN01_ReservaInativaNaoOcupaAData(ReservationStatus status)
    {
        var kiosk = await _data.KioskAsync();
        await _data.ReservationAsync(kiosk, new DateOnly(2026, 11, 20), status);

        var calendar = await GetCalendarAsync(kiosk.Id, 2026, 11);

        Assert.Equal("LIVRE", StatusOf(calendar, 2026, 11, 20));
    }

    [Fact]
    public async Task RN03_UsaADataDeBrasiliaParaAJanela()
    {
        // 10/10/2026 às 02:00 UTC ainda é 09/10/2026 às 23:00 em Brasília.
        _fixture.Clock.UtcNow = new DateTimeOffset(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);
        var kiosk = await _data.KioskAsync();

        var october = await GetCalendarAsync(kiosk.Id, 2026, 10);

        Assert.Equal("LIVRE", StatusOf(october, 2026, 10, 11));
    }

    [Fact]
    public async Task QuiosqueInativoOuInexistente_RetornaNaoEncontrado()
    {
        var inactive = await _data.KioskAsync(isActive: false);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/quiosques/{inactive.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/quiosques/{inactive.Id}/calendario?ano=2026&mes=11")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/quiosques/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task CalendarioComMesInvalido_RetornaRequisicaoInvalida()
    {
        var kiosk = await _data.KioskAsync();

        var response = await _client.GetAsync($"/api/quiosques/{kiosk.Id}/calendario?ano=2026&mes=13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Seed_CadastraOsQuiosquesDoPrototipo()
    {
        var kiosks = await _client.GetFromJsonAsync<List<KioskSummaryResponse>>("/api/quiosques");

        var expected = new (string Name, int Capacity, decimal DailyRate)[]
        {
            ("Quiosque das Mangueiras", 30, 150m), ("Pavilhão Central", 45, 200m), ("Quiosque dos Ipês", 20, 120m),
            ("Quiosque das Palmeiras", 50, 220m), ("Quiosque Jardim dos Pássaros", 25, 130m), ("Quiosque da Quadra", 35, 160m),
        };
        foreach (var (name, capacity, dailyRate) in expected)
        {
            Assert.Contains(kiosks!, k => k.Name == name && k.Capacity == capacity && k.DailyRate == dailyRate && k.Items.Count >= 2);
        }
    }

    private async Task<CalendarResponse> GetCalendarAsync(Guid kioskId, int year, int month) =>
        (await _client.GetFromJsonAsync<CalendarResponse>($"/api/quiosques/{kioskId}/calendario?ano={year}&mes={month}"))!;

    private static string StatusOf(CalendarResponse calendar, int year, int month, int day) =>
        calendar.Days.Single(d => d.Date == new DateOnly(year, month, day)).Status;

    private record KioskSummaryResponse(Guid Id, string Name, string PhotoUrl, int Capacity, decimal DailyRate, List<string> Items);

    private record KioskDetailsResponse(
        Guid Id, string Name, string Description, int Capacity, decimal DailyRate,
        string PhotoUrl, string UsageRules, List<string> Items, List<string> Photos);

    private record CalendarResponse(Guid KioskId, int Year, int Month, List<CalendarDayResponse> Days);

    private record CalendarDayResponse(DateOnly Date, string Status);
}
