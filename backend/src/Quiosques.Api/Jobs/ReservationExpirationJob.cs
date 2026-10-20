using Quiosques.Application.Reservations;

namespace Quiosques.Api.Jobs;

// RN05: a cada minuto, expira as reservas pendentes cujo prazo de 15 minutos terminou (ADR-001).
public partial class ReservationExpirationJob(IServiceScopeFactory scopeFactory, ILogger<ReservationExpirationJob> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var expired = await scope.ServiceProvider.GetRequiredService<ExpireOverdueReservations>().ExecuteAsync(stoppingToken);
                if (expired > 0)
                {
                    LogExpired(logger, expired);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Uma falha (ex.: banco fora do ar) não derruba o job; ele tenta de novo no próximo minuto.
                LogFailure(logger, exception);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} reserva(s) expirada(s) por falta de pagamento")]
    private static partial void LogExpired(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha ao expirar reservas")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
