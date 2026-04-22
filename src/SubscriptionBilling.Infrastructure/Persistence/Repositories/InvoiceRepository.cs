using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Infrastructure.Persistence.Repositories;

internal sealed class InvoiceRepository(BillingDbContext db) : IInvoiceRepository
{
    public Task Add(Invoice invoice, CancellationToken ct) =>
        db.Invoices.AddAsync(invoice, ct).AsTask();

    public Task<Invoice?> GetById(InvoiceId id, CancellationToken ct) =>
        db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Invoice>> ListByCustomer(CustomerId customerId, CancellationToken ct) =>
        await db.Invoices.Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.IssuedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Invoice>> ListBySubscription(SubscriptionId subscriptionId, CancellationToken ct) =>
        await db.Invoices.Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.IssuedAt).ToListAsync(ct);
}
