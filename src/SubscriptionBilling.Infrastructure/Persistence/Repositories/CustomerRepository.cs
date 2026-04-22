using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(BillingDbContext db) : ICustomerRepository
{
    public Task Add(Customer customer, CancellationToken ct) =>
        db.Customers.AddAsync(customer, ct).AsTask();

    public Task<Customer?> GetById(CustomerId id, CancellationToken ct) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
}
