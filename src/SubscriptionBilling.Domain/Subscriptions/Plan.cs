using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Shared;

namespace SubscriptionBilling.Domain.Subscriptions;

public sealed class Plan : ValueObject
{
    public string Name { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public BillingCycle Cycle { get; private set; }

    private Plan() { } // EF Core materialization

    private Plan(string name, Money price, BillingCycle cycle)
    {
        Name = name;
        Price = price;
        Cycle = cycle;
    }

    public static Plan Create(string name, Money price, BillingCycle cycle)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Plan name is required.");
        return new Plan(name.Trim(), price, cycle);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Price;
        yield return Cycle;
    }
}
