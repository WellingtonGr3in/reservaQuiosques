namespace Quiosques.Domain.Rules;

// RN07: o cancelamento só é permitido até 48h antes das 00:00 do dia reservado (horário de Brasília).
public static class CancellationPolicy
{
    public static readonly TimeSpan MinimumNotice = TimeSpan.FromHours(48);

    public const string DeadlinePassedMessage = "O cancelamento só é permitido até 48h antes da data reservada";

    public static DateTimeOffset DeadlineFor(DateOnly reservationDate) =>
        BrazilTime.StartOf(reservationDate) - MinimumNotice;
}
