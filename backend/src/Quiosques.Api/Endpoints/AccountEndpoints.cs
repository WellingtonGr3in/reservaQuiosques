using System.Security.Claims;
using Quiosques.Api.Auth;
using Quiosques.Application.Accounts;
using Quiosques.Application.Payments;
using Quiosques.Application.Reservations;

namespace Quiosques.Api.Endpoints;

// HU05: criar conta, fazer login e ver o histórico.
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        // RF09 / RN09: conta vinculada ao CPF de quem já fez reservas; o e-mail é o login. Já devolve o token.
        app.MapPost("/api/contas", async (CreateAccountRequest request, CreateAccount useCase, JwtTokenIssuer tokens, CancellationToken ct) =>
        {
            var account = await useCase.ExecuteAsync(
                new CreateAccountCommand(request.Cpf, request.Email, request.Password), ct);
            return Results.Json(ToResponse(tokens.Issue(account)), statusCode: StatusCodes.Status201Created);
        });

        // RF09
        app.MapPost("/api/login", async (LoginRequest request, Login useCase, JwtTokenIssuer tokens, CancellationToken ct) =>
            Results.Ok(ToResponse(tokens.Issue(await useCase.ExecuteAsync(request.Email, request.Password, ct)))));

        // Dados do cliente logado, para pré-preencher a reserva (CPF mascarado, RNF07).
        app.MapGet("/api/minha-conta", async (ClaimsPrincipal user, GetCustomerProfile useCase, CancellationToken ct) =>
            Results.Ok(await useCase.ExecuteAsync(JwtTokenIssuer.CustomerIdOf(user), ct)))
            .RequireAuthorization(AuthSetup.CustomerPolicy);

        var mine = app.MapGroup("/api/minhas-reservas").RequireAuthorization(AuthSetup.CustomerPolicy);

        // RF04 com login: mesmas regras da reserva sem conta, com o CPF da conta.
        mine.MapPost("/", async (MyReservationRequest request, ClaimsPrincipal user, CreateReservation useCase, CancellationToken ct) =>
        {
            var created = await useCase.ExecuteForCustomerAsync(
                JwtTokenIssuer.CustomerIdOf(user),
                new CreateReservationCommand(request.KioskId, request.Date, null, request.Name, request.Email, request.Phone),
                ct);
            return Results.Json(
                new ReservationEndpoints.ReservationResponse(
                    created.Code, created.KioskId, created.Date, created.Amount, ReservationEndpoints.ToApi(created.Status), created.ExpiresAt),
                statusCode: StatusCodes.Status201Created);
        });

        // RF05 com login: cobrança Pix de uma reserva do próprio cliente.
        mine.MapPost("/{code}/pagamento", async (string code, ClaimsPrincipal user, StartPayment useCase, CancellationToken ct) =>
        {
            var payment = await useCase.ExecuteForCustomerAsync(JwtTokenIssuer.CustomerIdOf(user), code, ct);
            return Results.Ok(new PaymentEndpoints.PixPaymentResponse(
                payment.ReservationCode, payment.Amount, payment.PixCopyPaste, payment.ExpiresAt, payment.TransactionId));
        });

        // RF10: histórico do cliente logado, incluindo reservas feitas antes de criar a conta (RN09).
        mine.MapGet("/", async (ClaimsPrincipal user, ListCustomerReservations useCase, CancellationToken ct) =>
            Results.Ok(ToResponse(await useCase.ExecuteAsync(JwtTokenIssuer.CustomerIdOf(user), ct))));

        // "Ver comprovante" / "Ver detalhes"
        mine.MapGet("/{code}", async (string code, ClaimsPrincipal user, GetCustomerReservation useCase, CancellationToken ct) =>
            Results.Ok(ReservationEndpoints.ToResponse(await useCase.ExecuteAsync(JwtTokenIssuer.CustomerIdOf(user), code, ct))));

        // RN07 também vale com login.
        mine.MapPost("/{code}/cancelamento", async (string code, ClaimsPrincipal user, CancelReservation useCase, CancellationToken ct) =>
            Results.Ok(ReservationEndpoints.ToResponse(await useCase.ExecuteForCustomerAsync(JwtTokenIssuer.CustomerIdOf(user), code, ct))));

        return app;
    }

    // "Realizada": reserva confirmada cuja data já passou. Só existe na exibição (docs/05-modelagem-dados.md).
    public const string CompletedStatus = "REALIZADA";

    private static TokenResponse ToResponse(AccessToken token) => new(token.Token, token.ExpiresAt);

    // Usado também pela consulta sem login por CPF (HU04).
    public static CustomerReservationsResponse ToResponse(CustomerReservations result) => new(
        result.HolderName,
        result.MaskedCpf,
        result.Reservations
            .Select(r => ReservationEndpoints.ToResponse(r.Details, r.Completed ? CompletedStatus : null))
            .ToList());

    public record CreateAccountRequest(string? Cpf, string? Email, string? Password);

    public record LoginRequest(string? Email, string? Password);

    public record MyReservationRequest(Guid KioskId, DateOnly Date, string? Name, string? Email, string? Phone);

    public record TokenResponse(string Token, DateTimeOffset ExpiresAt);

    public record CustomerReservationsResponse(
        string HolderName, string MaskedCpf, IReadOnlyList<ReservationEndpoints.ReservationDetailsResponse> Reservations);
}
