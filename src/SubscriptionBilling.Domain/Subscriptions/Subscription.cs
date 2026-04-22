using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions.Events;

namespace SubscriptionBilling.Domain.Subscriptions;

public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    public CustomerId CustomerId { get; private set; }
    public Plan Plan { get; private set; } = default!;
    public SubscriptionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// End of the current billing period. Drives when the next invoice is generated.
    /// Null until activation.
    /// </summary>
    public DateTimeOffset? NextBillingDate { get; private set; }

    private Subscription() { } // EF

    private Subscription(SubscriptionId id, CustomerId customerId, Plan plan, DateTimeOffset createdAt) : base(id)
    {
        CustomerId = customerId;
        Plan = plan;
        Status = SubscriptionStatus.PendingActivation;
        CreatedAt = createdAt;
    }

    public static Subscription Create(CustomerId customerId, Plan plan, DateTimeOffset now)
        => new(SubscriptionId.New(), customerId, plan, now);

    /// <summary>
    /// Activating a subscription generates the first invoice and emits SubscriptionActivated.
    /// </summary>
    public Invoice Activate(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new DomainException("Cancelled subscription cannot be activated.");
        if (Status == SubscriptionStatus.Active)
            throw new DomainException("Subscription is already active.");

        Status = SubscriptionStatus.Active;
        ActivatedAt = now;

        var periodEnd = Plan.Cycle.Next(now);
        var invoice = Invoice.Issue(Id, CustomerId, Plan.Price, now, periodEnd, now);
        NextBillingDate = periodEnd;

        Raise(new SubscriptionActivated(Id, CustomerId, now));
        return invoice;
    }

    /// <summary>
    /// Generates the next billing-cycle invoice. Returns null if the subscription
    /// is not active or it is not yet time to bill.
    /// </summary>
    public Invoice? GenerateNextInvoice(DateTimeOffset now)
    {
        if (Status != SubscriptionStatus.Active)
            return null;
        if (NextBillingDate is null || now < NextBillingDate)
            return null;

        var periodStart = NextBillingDate.Value;
        var periodEnd = Plan.Cycle.Next(periodStart);
        var invoice = Invoice.Issue(Id, CustomerId, Plan.Price, periodStart, periodEnd, now);
        NextBillingDate = periodEnd;
        return invoice;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new DomainException("Subscription is already cancelled.");

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = now;
        NextBillingDate = null;
    }
}
