using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Endpoints;

// HU02, HU03 e HU04: reservar, ver o comprovante, consultar e cancelar.
public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservas");

        // RF04
        group.MapPost("/", async (CreateReservationRequest request, CreateReservation useCase, CancellationToken ct) =>
        {
            var created = await useCase.ExecuteAsync(
                new CreateReservationCommand(request.KioskId, request.Date, request.Cpf, request.Name, request.Email, request.Phone),
                ct);

            return Results.Json(
                new ReservationResponse(created.Code, created.KioskId, created.Date, created.Amount, ToApi(created.Status), created.ExpiresAt),
                statusCode: StatusCodes.Status201Created);
        });

        // Tela "Comprovante de Reserva" (HU03). A tela de pagamento também consulta aqui até o status virar CONFIRMADA.
        group.MapPost("/{code}/comprovante", async (string code, PaymentEndpoints.CpfRequest request, GetReceipt useCase, CancellationToken ct) =>
        {
            var receipt = await useCase.ExecuteAsync(code, request.Cpf, ct);
            return Results.Ok(new ReceiptResponse(
                receipt.Code, receipt.KioskName, receipt.KioskCapacity, receipt.Date, receipt.StartTime, receipt.EndTime,
                receipt.HolderName, receipt.MaskedCpf, receipt.Amount, ToApi(receipt.Status)));
        });

        // RF08 / HU04: consulta sem login com código + CPF (RN10), limitada a 5 tentativas por minuto por IP (RNF06).
        group.MapPost("/consulta", async (LookupRequest request, GetReservationDetails useCase, CancellationToken ct) =>
                Results.Ok(ToResponse(await useCase.ExecuteAsync(request.Code, request.Cpf, ct))))
            .RequireRateLimiting(RateLimiting.ReservationLookupPolicy);

        // RF08 / HU04: cancelamento sem login com código + CPF (RN07, RN10), com o mesmo limite.
        group.MapPost("/{code}/cancelamento", async (string code, PaymentEndpoints.CpfRequest request, CancelReservation useCase, CancellationToken ct) =>
                Results.Ok(ToResponse(await useCase.ExecuteAsync(code, request.Cpf, ct))))
            .RequireRateLimiting(RateLimiting.ReservationLookupPolicy);

        return app;
    }

    public static ReservationDetailsResponse ToResponse(ReservationDetails details, string? statusOverride = null)
    {
        var receipt = details.Receipt;
        return new ReservationDetailsResponse(
            receipt.Code, receipt.KioskName, receipt.KioskCapacity, details.KioskPhotoUrl, receipt.Date, receipt.StartTime, receipt.EndTime,
            receipt.HolderName, receipt.MaskedCpf, receipt.Amount, statusOverride ?? ToApi(receipt.Status), details.CreatedAt,
            details.CancellationDeadline, details.CanCancel);
    }

    public static string ToApi(ReservationStatus status) => status switch
    {
        ReservationStatus.PendingPayment => "PENDENTE_PAGAMENTO",
        ReservationStatus.Confirmed => "CONFIRMADA",
        ReservationStatus.Expired => "EXPIRADA",
        ReservationStatus.Cancelled => "CANCELADA",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public record CreateReservationRequest(Guid KioskId, DateOnly Date, string? Cpf, string? Name, string? Email, string? Phone);

    public record ReceiptResponse(
        string Code, string KioskName, int KioskCapacity, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
        string HolderName, string MaskedCpf, decimal Amount, string Status);

    public record ReservationResponse(string Code, Guid KioskId, DateOnly Date, decimal Amount, string Status, DateTimeOffset ExpiresAt);

    public record LookupRequest(string? Code, string? Cpf);

    public record ReservationDetailsResponse(
        string Code, string KioskName, int KioskCapacity, string KioskPhotoUrl, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
        string HolderName, string MaskedCpf, decimal Amount, string Status, DateTimeOffset CreatedAt,
        DateTimeOffset CancellationDeadline, bool CanCancel);
}
