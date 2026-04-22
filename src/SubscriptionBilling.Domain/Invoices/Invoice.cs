using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices.Events;
using SubscriptionBilling.Domain.Shared;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Domain.Invoices;

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    public SubscriptionId SubscriptionId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public InvoiceStatus Status { get; private set; }

    private Invoice() { } // EF

    private Invoice(
        InvoiceId id,
        SubscriptionId subscriptionId,
        CustomerId customerId,
        Money amount,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset issuedAt) : base(id)
    {
        SubscriptionId = subscriptionId;
        CustomerId = customerId;
        Amount = amount;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        IssuedAt = issuedAt;
        Status = InvoiceStatus.Pending;
    }

    internal static Invoice Issue(
        SubscriptionId subscriptionId,
        CustomerId customerId,
        Money amount,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset issuedAt)
    {
        if (periodEnd <= periodStart)
            throw new DomainException("Invoice period end must be after period start.");

        var invoice = new Invoice(InvoiceId.New(), subscriptionId, customerId, amount, periodStart, periodEnd, issuedAt);
        invoice.Raise(new InvoiceGenerated(invoice.Id, subscriptionId, customerId, amount, periodStart, periodEnd));
        return invoice;
    }

    public void Pay(DateTimeOffset paidAt)
    {
        if (Status == InvoiceStatus.Paid)
            throw new DomainException($"Invoice {Id} is already paid.");

        Status = InvoiceStatus.Paid;
        PaidAt = paidAt;
        Raise(new PaymentReceived(Id, Amount, paidAt));
    }
}
