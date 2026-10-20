using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quiosques.Application.Common;
using Quiosques.Application.Notifications;
using Quiosques.Application.Reservations;
using Quiosques.Infrastructure.Payments;
using Quiosques.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Quiosques.Api.Tests.Infrastructure;

// Sobe um PostgreSQL real em container, aplica as migrations e expõe a API
// apontando para ele, uma vez para toda a coleção de testes.
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public FakeClock Clock { get; } = new();

    public WebApplicationFactory<Program> Api { get; private set; } = null!;

    public QuiosquesDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<QuiosquesDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        Api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // O job de expiração (RN05) fica desligado; os testes chamam a expiração diretamente.
            builder.UseSetting("Jobs:ReservationExpiration:Enabled", "false");
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Quiosques"] = ConnectionString,
                    // RF11: administrador criado na subida da API.
                    ["Admin:Name"] = "Admin de Teste",
                    ["Admin:Email"] = AdminEmail,
                    ["Admin:Password"] = AdminPassword,
                    // RF05: copia-e-cola no padrão BR Code (Pix estático); a confirmação continua simulada.
                    ["Pix:Key"] = "+5547999998888",
                    ["Pix:MerchantName"] = "Condominio Teste",
                    ["Pix:MerchantCity"] = "Joinville",
                    // RF16: fotos enviadas pelo admin vão para uma pasta temporária.
                    ["Storage:ImagesPath"] = ImagesPath,
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock>(Clock);
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
                services.AddSingleton<IStartupFilter, ClientIpStartupFilter>();
            });
        });
    }

    public string ImagesPath { get; } = Path.Combine(Path.GetTempPath(), $"quiosques-uploads-{Guid.NewGuid():N}");

    public const string AdminEmail = "admin@teste.local";
    public const string AdminPassword = "senha-admin-teste";

    public RecordingEmailSender Emails { get; } = new();

    // Cliente com um IP próprio, para o limite de tentativas por IP (RNF06) não vazar entre testes.
    public HttpClient CreateClientWithOwnIp()
    {
        var client = Api.CreateClient();
        var n = Interlocked.Increment(ref _clientIpSequence);
        client.DefaultRequestHeaders.Add(ClientIpStartupFilter.Header, $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}");
        return client;
    }

    private int _clientIpSequence;

    public FakePaymentGateway Gateway => Api.Services.GetRequiredService<FakePaymentGateway>();

    public async Task<int> ExpireOverdueReservationsAsync()
    {
        await using var scope = Api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ExpireOverdueReservations>().ExecuteAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        await _container.DisposeAsync();
    }
}

public class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}

// O TestServer não tem IP de origem; este filtro usa o IP informado no cabeçalho do teste.
public class ClientIpStartupFilter : IStartupFilter
{
    public const string Header = "X-Test-Client-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var ip))
            {
                context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ip.ToString());
            }

            await nextMiddleware();
        });
        next(app);
    };
}

// Guarda os comprovantes "enviados" para os testes conferirem (RF07).
public class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<(string To, ReservationReceipt Receipt)> _sent = new();

    public IReadOnlyList<(string To, ReservationReceipt Receipt)> SentFor(string reservationCode) =>
        _sent.Where(e => e.Receipt.Code == reservationCode).ToList();

    public Task SendReservationReceiptAsync(string to, ReservationReceipt receipt, CancellationToken cancellationToken)
    {
        _sent.Enqueue((to, receipt));
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
