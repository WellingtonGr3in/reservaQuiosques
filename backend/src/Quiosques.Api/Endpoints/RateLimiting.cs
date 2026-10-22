using System.Threading.RateLimiting;

namespace Quiosques.Api.Endpoints;

// RNF06: a consulta por CPF (e o cancelamento sem login) aceita no máximo 5 tentativas por minuto por IP.
public static class RateLimiting
{
    public const string ReservationLookupPolicy = "consulta-reserva";
    public const int PermitLimit = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public const string TooManyAttemptsMessage = "Muitas tentativas. Aguarde um minuto e tente novamente.";

    public static IServiceCollection AddReservationLookupRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
                await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponse(TooManyAttemptsMessage), cancellationToken);

            // Janela deslizante: não permite 10 tentativas seguidas na virada de um minuto para o outro.
            options.AddPolicy(ReservationLookupPolicy, httpContext => RateLimitPartition.GetSlidingWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = Window,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                }));
        });
}
