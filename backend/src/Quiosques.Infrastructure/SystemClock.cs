using Quiosques.Application.Common;

namespace Quiosques.Infrastructure;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
