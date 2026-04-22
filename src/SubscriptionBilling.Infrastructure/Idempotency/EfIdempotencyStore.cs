using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Idempotency;

internal sealed class EfIdempotencyStore(BillingDbContext db) : IIdempotencyStore
{
    public async Task<string?> TryGet(string key, CancellationToken ct)
    {
        var record = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key, ct);
        return record?.Result;
    }

    public async Task Save(string key, string serializedResult, CancellationToken ct)
    {
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = key,
            Result = serializedResult,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
