using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Outbox;

public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processor batch failed");
            }
            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var pending = await db.Outbox
            .Where(m => m.ProcessedOn == null)
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var msg in pending)
        {
            try
            {
                var type = Type.GetType(msg.Type)
                    ?? throw new InvalidOperationException($"Unknown event type {msg.Type}");
                if (JsonSerializer.Deserialize(msg.Payload, type, OutboxJsonOptions.Default) is not IDomainEvent ev)
                    throw new InvalidOperationException($"Payload for {msg.Id} did not deserialize.");
                await dispatcher.Dispatch(ev, ct);
                msg.ProcessedOn = DateTimeOffset.UtcNow;
                msg.Error = null;
            }
            catch (Exception ex)
            {
                msg.Error = ex.ToString();
                logger.LogError(ex, "Failed to dispatch outbox message {Id}", msg.Id);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
