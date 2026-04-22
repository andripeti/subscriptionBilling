using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository(BillingDbContext db) : ISubscriptionRepository
{
    public Task Add(Subscription subscription, CancellationToken ct) =>
        db.Subscriptions.AddAsync(subscription, ct).AsTask();

    public Task<Subscription?> GetById(SubscriptionId id, CancellationToken ct) =>
        db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Subscription>> GetActiveDue(DateTimeOffset asOf, CancellationToken ct) =>
        await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                        && s.NextBillingDate != null
                        && s.NextBillingDate <= asOf)
            .ToListAsync(ct);
}
