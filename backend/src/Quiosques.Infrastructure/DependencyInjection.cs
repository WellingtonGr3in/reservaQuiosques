using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quiosques.Application.Accounts;
using Quiosques.Application.Admin;
using Quiosques.Application.Common;
using Quiosques.Application.Customers;
using Quiosques.Application.Kiosks;
using Quiosques.Application.Notifications;
using Quiosques.Application.Payments;
using Quiosques.Application.Reservations;
using Quiosques.Infrastructure.Notifications;
using Quiosques.Infrastructure.Payments;
using Quiosques.Infrastructure.Persistence;
using Quiosques.Infrastructure.Persistence.Repositories;
using Quiosques.Infrastructure.Security;
using Quiosques.Infrastructure.Storage;

namespace Quiosques.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // A connection string é lida ao criar o contexto, para que os testes possam substituí-la.
        services.AddDbContext<QuiosquesDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString("Quiosques")
                ?? throw new InvalidOperationException("Connection string 'Quiosques' não configurada.");
            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IKioskRepository, KioskRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAdministratorRepository, AdministratorRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        services.AddOptions<PixSettings>().BindConfiguration(PixSettings.Section);
        services.AddOptions<ImageStorageSettings>().BindConfiguration(ImageStorageSettings.Section);
        services.AddSingleton<IImageStorage, LocalImageStorage>();
        services.AddSingleton<FakePaymentGateway>();
        services.AddSingleton<IPaymentGateway>(provider => provider.GetRequiredService<FakePaymentGateway>());
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }

    // Aplica as migrations pendentes (usado quando a API sobe no Docker).
    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<QuiosquesDbContext>().Database.MigrateAsync();
    }
}
