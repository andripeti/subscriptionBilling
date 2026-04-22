using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Shared;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Domain.Invoices.Events;

public sealed record InvoiceGenerated(
    InvoiceId InvoiceId,
    SubscriptionId SubscriptionId,
    CustomerId CustomerId,
    Money Amount,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
