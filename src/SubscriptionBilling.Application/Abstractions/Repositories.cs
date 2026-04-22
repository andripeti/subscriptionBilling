using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Abstractions;

public interface ICustomerRepository
{
    Task Add(Customer customer, CancellationToken ct);
    Task<Customer?> GetById(CustomerId id, CancellationToken ct);
}

public interface ISubscriptionRepository
{
    Task Add(Subscription subscription, CancellationToken ct);
    Task<Subscription?> GetById(SubscriptionId id, CancellationToken ct);
    Task<IReadOnlyList<Subscription>> GetActiveDue(DateTimeOffset asOf, CancellationToken ct);
}

public interface IInvoiceRepository
{
    Task Add(Invoice invoice, CancellationToken ct);
    Task<Invoice?> GetById(InvoiceId id, CancellationToken ct);
    Task<IReadOnlyList<Invoice>> ListByCustomer(CustomerId customerId, CancellationToken ct);
    Task<IReadOnlyList<Invoice>> ListBySubscription(SubscriptionId subscriptionId, CancellationToken ct);
}
