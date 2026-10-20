using Microsoft.Extensions.FileProviders;
using Quiosques.Api.Auth;
using Quiosques.Api.Endpoints;
using Quiosques.Api.Jobs;
using Quiosques.Application;
using Quiosques.Application.Admin;
using Quiosques.Infrastructure;
using Quiosques.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddReservationLookupRateLimit();
builder.Services.AddJwtAuthentication(builder.Configuration);

// Corpo inválido vira exceção em todos os ambientes, para o ApiExceptionHandler responder com { "message" }.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// RN05. Os testes desligam o job e chamam a expiração diretamente.
if (builder.Configuration.GetValue("Jobs:ReservationExpiration:Enabled", defaultValue: true))
{
    builder.Services.AddHostedService<ReservationExpirationJob>();
}

var app = builder.Build();

// No Docker (docker-compose.yml) a API cria/atualiza o banco ao subir. Fora dele, use "dotnet ef database update".
if (app.Configuration.GetValue("Database:MigrateOnStartup", defaultValue: false))
{
    await app.Services.MigrateDatabaseAsync();
}

// RF11: cria o primeiro administrador com os dados da configuração (Admin:Email e Admin:Password), se ainda não existir.
if (app.Configuration["Admin:Email"] is { Length: > 0 } adminEmail && app.Configuration["Admin:Password"] is { Length: > 0 } adminPassword)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<EnsureDefaultAdministrator>()
        .ExecuteAsync(app.Configuration["Admin:Name"] ?? "Administrador", adminEmail, adminPassword, CancellationToken.None);
}

app.UseExceptionHandler();

// Fotos de quiosque enviadas pelo administrador (RF16), servidas em /uploads.
var imagesPath = Path.GetFullPath(app.Configuration[$"{ImageStorageSettings.Section}:ImagesPath"] ?? new ImageStorageSettings().ImagesPath);
Directory.CreateDirectory(imagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = ImageStorageSettings.PublicPath,
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapKioskEndpoints();
app.MapReservationEndpoints();
app.MapPaymentEndpoints(app.Environment);
app.MapAccountEndpoints();
app.MapAdminEndpoints();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
