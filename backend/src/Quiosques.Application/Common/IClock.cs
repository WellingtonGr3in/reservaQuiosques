using Quiosques.Domain.Rules;

namespace Quiosques.Application.Common;

// Fonte única de data e hora. Nunca use DateTime.Now direto (CLAUDE.md).
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public static class ClockExtensions
{
    // RN03 e RN07 usam o horário de Brasília.
    public static DateOnly TodayInSaoPaulo(this IClock clock) => BrazilTime.DateOf(clock.UtcNow);
}
