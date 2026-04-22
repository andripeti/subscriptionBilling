using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Application.Customers.Commands;

public sealed record CreateCustomerCommand(string Name, string Email) : ICommand<Guid>;

public sealed class CreateCustomerHandler(
    ICustomerRepository customers,
    IUnitOfWork uow,
    IClock clock) : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var email = Email.Create(cmd.Email);
        var customer = Customer.Create(cmd.Name, email, clock.UtcNow);
        await customers.Add(customer, ct);
        await uow.SaveChangesAsync(ct);
        return customer.Id.Value;
    }
}
