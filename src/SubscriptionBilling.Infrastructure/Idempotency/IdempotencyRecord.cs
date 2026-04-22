namespace SubscriptionBilling.Infrastructure.Idempotency;

public sealed class IdempotencyRecord
{
    public string Key { get; init; } = default!;
    public string Result { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
}
