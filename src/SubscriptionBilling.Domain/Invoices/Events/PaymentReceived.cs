using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Shared;

namespace SubscriptionBilling.Domain.Invoices.Events;

public sealed record PaymentReceived(
    InvoiceId InvoiceId,
    Money Amount,
    DateTimeOffset PaidAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = PaidAt;
}
