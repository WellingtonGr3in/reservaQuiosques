using Quiosques.Application.Kiosks;
using Quiosques.Domain.Rules;

namespace Quiosques.Api.Endpoints;

// HU01: ver quiosques e disponibilidade.
public static class KioskEndpoints
{
    public static IEndpointRouteBuilder MapKioskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quiosques");

        // RF01
        group.MapGet("/", async (ListKiosks useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(ct)));

        // RF02
        group.MapGet("/{id:guid}", async (Guid id, GetKioskDetails useCase, CancellationToken ct) =>
            await useCase.ExecuteAsync(id, ct) is { } details
                ? Results.Ok(details)
                : Results.NotFound(new ErrorResponse("Quiosque não encontrado")));

        // RF03
        group.MapGet("/{id:guid}/calendario", async (Guid id, int ano, int mes, GetKioskCalendar useCase, CancellationToken ct) =>
        {
            if (mes is < 1 or > 12 || ano is < 2000 or > 2100)
            {
                return Results.BadRequest(new ErrorResponse("Mês ou ano inválido"));
            }

            var calendar = await useCase.ExecuteAsync(id, ano, mes, ct);
            if (calendar is null)
            {
                return Results.NotFound(new ErrorResponse("Quiosque não encontrado"));
            }

            return Results.Ok(new CalendarResponse(
                calendar.KioskId,
                calendar.Year,
                calendar.Month,
                calendar.Days.Select(d => new CalendarDayResponse(d.Date, ToApi(d.Availability))).ToList()));
        });

        return app;
    }

    private static string ToApi(DayAvailability availability) => availability switch
    {
        DayAvailability.Free => "LIVRE",
        DayAvailability.Occupied => "OCUPADO",
        DayAvailability.Unavailable => "INDISPONIVEL",
        _ => throw new ArgumentOutOfRangeException(nameof(availability), availability, null),
    };

    public record CalendarResponse(Guid KioskId, int Year, int Month, IReadOnlyList<CalendarDayResponse> Days);

    public record CalendarDayResponse(DateOnly Date, string Status);
}
