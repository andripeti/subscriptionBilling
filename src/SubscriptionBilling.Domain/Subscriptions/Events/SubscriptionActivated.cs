using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Domain.Subscriptions.Events;

public sealed record SubscriptionActivated(
    SubscriptionId SubscriptionId,
    CustomerId CustomerId,
    DateTimeOffset ActivatedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = ActivatedAt;
}
