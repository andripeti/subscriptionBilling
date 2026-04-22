using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Infrastructure.Outbox;

/// <summary>
/// Atomic outbox: before the DbContext commits, lift every aggregate root's pending
/// domain events into the OutboxMessages table in the same transaction.
/// </summary>
public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, ct);

        // Snapshot the tracked entries before we add outbox messages (which would mutate the tracker).
        var aggregates = ctx.ChangeTracker.Entries()
            .Select(e => e.Entity)
            .ToList(); // materialize BEFORE adding anything

        var events = new List<IDomainEvent>();

        foreach (var entity in aggregates)
        {
            if (entity is not AggregateRootBase agg) continue;
            events.AddRange(agg.GetAndClearEvents());
        }

        foreach (var ev in events)
        {
            ctx.Set<OutboxMessage>().Add(new OutboxMessage
            {
                Id = ev.EventId,
                Type = ev.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(ev, ev.GetType(), OutboxJsonOptions.Default),
                OccurredOn = ev.OccurredOn
            });
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
