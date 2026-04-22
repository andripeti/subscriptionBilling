using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Billing;

/// <summary>
/// Generates the next invoice for every active subscription whose NextBillingDate
/// has passed. Designed to be called by a background job (e.g. hosted worker).
/// </summary>
public sealed class BillingCycleService(
    ISubscriptionRepository subscriptions,
    IInvoiceRepository invoices,
    IUnitOfWork uow,
    IClock clock) : IBillingCycleService
{
    public async Task<int> RunOnce(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await subscriptions.GetActiveDue(now, ct);
        var generated = 0;

        foreach (var subscription in due)
        {
            // GenerateNextInvoice may return null if billing isn't due yet — defensive against
            // race conditions between the query and the in-memory state.
            var invoice = subscription.GenerateNextInvoice(now);
            if (invoice is null) continue;

            await invoices.Add(invoice, ct);
            generated++;
        }

        if (generated > 0)
            await uow.SaveChangesAsync(ct);

        return generated;
    }
}
