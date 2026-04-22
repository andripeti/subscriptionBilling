using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.Customers;

public sealed class Customer : AggregateRoot<CustomerId>
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Customer() { } // EF

    private Customer(CustomerId id, string name, Email email, DateTimeOffset createdAt)
        : base(id)
    {
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    public static Customer Create(string name, Email email, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Customer name is required.");
        return new Customer(CustomerId.New(), name.Trim(), email, now);
    }
}
