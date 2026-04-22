using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
