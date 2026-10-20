namespace Quiosques.Domain.Rules;

// RN02: a diária corresponde ao horário de uso do condomínio, das 08:00 às 22:00.
public static class DailyUsage
{
    public static readonly TimeOnly Start = new(8, 0);
    public static readonly TimeOnly End = new(22, 0);
}
