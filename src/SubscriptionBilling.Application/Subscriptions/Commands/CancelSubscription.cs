using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed record CancelSubscriptionCommand(Guid SubscriptionId) : ICommand<Unit>;

public sealed class CancelSubscriptionHandler(
    ISubscriptionRepository subscriptions,
    IUnitOfWork uow,
    IClock clock) : ICommandHandler<CancelSubscriptionCommand, Unit>
{
    public async Task<Unit> Handle(CancelSubscriptionCommand cmd, CancellationToken ct)
    {
        var subscription = await subscriptions.GetById(new SubscriptionId(cmd.SubscriptionId), ct)
            ?? throw new DomainException($"Subscription {cmd.SubscriptionId} not found.");
        subscription.Cancel(clock.UtcNow);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
