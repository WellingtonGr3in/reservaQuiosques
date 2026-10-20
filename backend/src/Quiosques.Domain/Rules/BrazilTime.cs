namespace Quiosques.Domain.Rules;

// RN03 e RN07 usam o horário de Brasília.
public static class BrazilTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateOnly DateOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    // Instante em que o dia começa (00:00) em Brasília.
    public static DateTimeOffset StartOf(DateOnly date) =>
        new(TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), Zone));
}
