namespace SubscriptionBilling.Domain.Subscriptions;

public readonly record struct SubscriptionId(Guid Value)
{
    public static SubscriptionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
