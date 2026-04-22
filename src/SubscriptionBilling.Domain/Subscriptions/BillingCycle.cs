namespace SubscriptionBilling.Domain.Subscriptions;

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}

public static class BillingCycleExtensions
{
    public static DateTimeOffset Next(this BillingCycle cycle, DateTimeOffset from) => cycle switch
    {
        BillingCycle.Monthly => from.AddMonths(1),
        BillingCycle.Yearly => from.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(cycle))
    };
}
