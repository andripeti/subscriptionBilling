using Microsoft.Extensions.Logging;
using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Infrastructure.Outbox;

public interface IDomainEventDispatcher
{
    Task Dispatch(IDomainEvent @event, CancellationToken ct);
}

/// <summary>
/// Default no-op dispatcher: replace with a real bus (e.g. MassTransit, SQS) in production.
/// Logs the event so you can see it flow through during development.
/// </summary>
internal sealed class LoggingDomainEventDispatcher(
    ILogger<LoggingDomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public Task Dispatch(IDomainEvent @event, CancellationToken ct)
    {
        logger.LogInformation("Dispatched domain event {EventType} {EventId}",
            @event.GetType().Name, @event.EventId);
        return Task.CompletedTask;
    }
}
