namespace SubscriptionBilling.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTimeOffset OccurredOn { get; init; }
    public DateTimeOffset? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
