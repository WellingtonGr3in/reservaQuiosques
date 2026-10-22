using Microsoft.Extensions.DependencyInjection;
using Quiosques.Application.Accounts;
using Quiosques.Application.Admin;
using Quiosques.Application.Kiosks;
using Quiosques.Application.Payments;
using Quiosques.Application.Reservations;

namespace Quiosques.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListKiosks>();
        services.AddScoped<GetKioskDetails>();
        services.AddScoped<GetKioskCalendar>();
        services.AddScoped<CreateReservation>();
        services.AddScoped<StartPayment>();
        services.AddScoped<ConfirmPayment>();
        services.AddScoped<ExpireOverdueReservations>();
        services.AddScoped<GetReceipt>();
        services.AddScoped<CancelReservation>();
        services.AddScoped<CreateAccount>();
        services.AddScoped<Login>();
        services.AddScoped<ListCustomerReservations>();
        services.AddScoped<GetCustomerReservation>();
        services.AddScoped<GetCustomerProfile>();

        // Área de administração (HU06, HU07).
        services.AddScoped<AdministratorLogin>();
        services.AddScoped<EnsureDefaultAdministrator>();
        services.AddScoped<ListReservationsForAdministrator>();
        services.AddScoped<GetReservationForAdministrator>();
        services.AddScoped<RescheduleReservation>();
        services.AddScoped<ListKiosksForAdministrator>();
        services.AddScoped<GetKioskForAdministrator>();
        services.AddScoped<SaveKiosk>();
        services.AddScoped<DeleteKiosk>();
        services.AddScoped<UploadKioskImage>();

        return services;
    }
}
