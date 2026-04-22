using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Shared;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    string PlanName,
    decimal PriceAmount,
    string PriceCurrency,
    BillingCycle Cycle) : ICommand<CreateSubscriptionResult>;

public sealed record CreateSubscriptionResult(Guid SubscriptionId, Guid FirstInvoiceId);

public sealed class CreateSubscriptionHandler(
    ICustomerRepository customers,
    ISubscriptionRepository subscriptions,
    IInvoiceRepository invoices,
    IUnitOfWork uow,
    IClock clock) : ICommandHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
    {
        var customerId = new CustomerId(cmd.CustomerId);
        var customer = await customers.GetById(customerId, ct)
            ?? throw new DomainException($"Customer {cmd.CustomerId} not found.");

        var plan = Plan.Create(cmd.PlanName, Money.Of(cmd.PriceAmount, cmd.PriceCurrency), cmd.Cycle);
        var subscription = Subscription.Create(customer.Id, plan, clock.UtcNow);

        // Activating generates the first invoice — per the business rule.
        var firstInvoice = subscription.Activate(clock.UtcNow);

        await subscriptions.Add(subscription, ct);
        await invoices.Add(firstInvoice, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateSubscriptionResult(subscription.Id.Value, firstInvoice.Id.Value);
    }
}
