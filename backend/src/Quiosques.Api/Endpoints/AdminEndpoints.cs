using Quiosques.Api.Auth;
using Quiosques.Application.Admin;
using Quiosques.Application.Common;
using Quiosques.Application.Reservations;
using Quiosques.Domain.Entities;

namespace Quiosques.Api.Endpoints;

// HU06 e HU07: área de administração (reservas e quiosques).
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // RF11
        app.MapPost("/api/admin/login", async (AccountEndpoints.LoginRequest request, AdministratorLogin useCase, JwtTokenIssuer tokens, CancellationToken ct) =>
        {
            var administrator = await useCase.ExecuteAsync(request.Email, request.Password, ct);
            var token = tokens.IssueForAdministrator(administrator);
            return Results.Ok(new AdminTokenResponse(token.Token, token.ExpiresAt, administrator.Name));
        });

        var admin = app.MapGroup("/api/admin").RequireAuthorization(AuthSetup.AdministratorPolicy);

        // ---------- Reservas (HU06) ----------

        // RF12. POST com corpo para o CPF do filtro não ir para a URL (nem para os logs).
        admin.MapPost("/reservas/busca", async (SearchRequest request, ListReservationsForAdministrator useCase, CancellationToken ct) =>
        {
            var search = new ReservationSearch(request.From, request.To, request.KioskId, ParseStatus(request.Status), request.Cpf);
            return Results.Ok((await useCase.ExecuteAsync(search, ct)).Select(ToResponse).ToList());
        });

        admin.MapGet("/reservas/{code}", async (string code, GetReservationForAdministrator useCase, CancellationToken ct) =>
            Results.Ok(ToResponse(await useCase.ExecuteAsync(code, ct))));

        // RF13 / RN13 / RN14
        admin.MapPost("/reservas", async (CreateRequest request, CreateReservation create, GetReservationForAdministrator get, CancellationToken ct) =>
        {
            var created = await create.ExecuteForAdministratorAsync(
                new CreateReservationCommand(request.KioskId, request.Date, request.Cpf, request.Name, request.Email, request.Phone),
                request.Paid,
                ct);
            return Results.Json(ToResponse(await get.ExecuteAsync(created.Code, ct)), statusCode: StatusCodes.Status201Created);
        });

        // RF14 / RN13
        admin.MapPut("/reservas/{code}", async (string code, RescheduleRequest request, RescheduleReservation reschedule, GetReservationForAdministrator get, CancellationToken ct) =>
        {
            await reschedule.ExecuteAsync(code, request.KioskId, request.Date, ct);
            return Results.Ok(ToResponse(await get.ExecuteAsync(code, ct)));
        });

        // RF13: Pix (QR Code e copia-e-cola) de uma reserva aguardando pagamento, para mostrar ao cliente.
        admin.MapPost("/reservas/{code}/pagamento", async (string code, Application.Payments.StartPayment useCase, CancellationToken ct) =>
        {
            var payment = await useCase.ExecuteForAdministratorAsync(code, ct);
            return Results.Ok(new PaymentEndpoints.PixPaymentResponse(
                payment.ReservationCode, payment.Amount, payment.PixCopyPaste, payment.ExpiresAt, payment.TransactionId));
        });

        // RF15 / RN13
        admin.MapPost("/reservas/{code}/cancelamento", async (string code, CancelReservation cancel, GetReservationForAdministrator get, CancellationToken ct) =>
        {
            await cancel.ExecuteForAdministratorAsync(code, ct);
            return Results.Ok(ToResponse(await get.ExecuteAsync(code, ct)));
        });

        // ---------- Quiosques (HU07 / RF16) ----------

        admin.MapGet("/quiosques", async (ListKiosksForAdministrator useCase, CancellationToken ct) =>
            Results.Ok((await useCase.ExecuteAsync(ct)).Select(ToResponse).ToList()));

        admin.MapGet("/quiosques/{id:guid}", async (Guid id, GetKioskForAdministrator useCase, CancellationToken ct) =>
            Results.Ok(ToResponse(await useCase.ExecuteAsync(id, ct))));

        admin.MapPost("/quiosques", async (KioskRequest request, SaveKiosk save, GetKioskForAdministrator get, CancellationToken ct) =>
        {
            var id = await save.ExecuteAsync(null, ToCommand(request), ct);
            return Results.Json(ToResponse(await get.ExecuteAsync(id, ct)), statusCode: StatusCodes.Status201Created);
        });

        admin.MapPut("/quiosques/{id:guid}", async (Guid id, KioskRequest request, SaveKiosk save, GetKioskForAdministrator get, CancellationToken ct) =>
        {
            await save.ExecuteAsync(id, ToCommand(request), ct);
            return Results.Ok(ToResponse(await get.ExecuteAsync(id, ct)));
        });

        // RN15
        admin.MapDelete("/quiosques/{id:guid}", async (Guid id, DeleteKiosk delete, CancellationToken ct) =>
        {
            await delete.ExecuteAsync(id, ct);
            return Results.NoContent();
        });

        // RF16: envio de foto do quiosque (campo "arquivo" do formulário). Devolve o endereço para usar como capa ou na galeria.
        // Sem antiforgery: a autenticação é por token no cabeçalho, não por cookie.
        admin.MapPost("/imagens", async (IFormFile arquivo, UploadKioskImage upload, CancellationToken ct) =>
        {
            await using var content = arquivo.OpenReadStream();
            var url = await upload.ExecuteAsync(content, arquivo.Length, ct);
            return Results.Json(new ImageResponse(url), statusCode: StatusCodes.Status201Created);
        }).DisableAntiforgery();

        return app;
    }

    private static ReservationStatus? ParseStatus(string? status) => status switch
    {
        null or "" => null,
        "PENDENTE_PAGAMENTO" => ReservationStatus.PendingPayment,
        "CONFIRMADA" => ReservationStatus.Confirmed,
        "EXPIRADA" => ReservationStatus.Expired,
        "CANCELADA" => ReservationStatus.Cancelled,
        _ => throw new ValidationException("Status inválido"),
    };

    private static AdminReservationResponse ToResponse(AdminReservation reservation) => new(
        ReservationEndpoints.ToResponse(reservation.Details, reservation.Completed ? AccountEndpoints.CompletedStatus : null),
        reservation.KioskId,
        reservation.CustomerEmail,
        reservation.CustomerPhone,
        reservation.PaidWith switch
        {
            PaymentMethod.Pix => "PIX",
            PaymentMethod.Administration => "ADMINISTRACAO",
            _ => null,
        },
        reservation.IsActive);

    private static AdminKioskResponse ToResponse(AdminKiosk kiosk) => new(
        kiosk.Details.Id, kiosk.Details.Name, kiosk.Details.Description, kiosk.Details.Capacity, kiosk.Details.DailyRate,
        kiosk.Details.PhotoUrl, kiosk.Details.UsageRules, kiosk.Details.Items, kiosk.Details.Photos, kiosk.IsActive, kiosk.HasReservations);

    private static SaveKioskCommand ToCommand(KioskRequest request) => new(
        request.Name, request.Description, request.Capacity, request.DailyRate, request.PhotoUrl, request.UsageRules,
        request.IsActive, request.Items, request.Photos);

    public record AdminTokenResponse(string Token, DateTimeOffset ExpiresAt, string Name);

    public record ImageResponse(string Url);

    public record SearchRequest(DateOnly? From, DateOnly? To, Guid? KioskId, string? Status, string? Cpf);

    public record CreateRequest(Guid KioskId, DateOnly Date, string? Cpf, string? Name, string? Email, string? Phone, bool Paid);

    public record RescheduleRequest(Guid KioskId, DateOnly Date);

    public record AdminReservationResponse(
        ReservationEndpoints.ReservationDetailsResponse Reservation,
        Guid KioskId,
        string CustomerEmail,
        string CustomerPhone,
        string? PaidWith,
        bool IsActive);

    public record KioskRequest(
        string? Name, string? Description, int Capacity, decimal DailyRate, string? PhotoUrl, string? UsageRules,
        bool IsActive, IReadOnlyList<string>? Items, IReadOnlyList<string>? Photos);

    public record AdminKioskResponse(
        Guid Id, string Name, string Description, int Capacity, decimal DailyRate, string PhotoUrl, string UsageRules,
        IReadOnlyList<string> Items, IReadOnlyList<string> Photos, bool IsActive, bool HasReservations);
}
