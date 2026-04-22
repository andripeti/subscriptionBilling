using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Invoices;

namespace SubscriptionBilling.Application.Invoices.Commands;

public sealed record PayInvoiceCommand(Guid InvoiceId) : ICommand<Unit>;

public sealed class PayInvoiceHandler(
    IInvoiceRepository invoices,
    IUnitOfWork uow,
    IClock clock) : ICommandHandler<PayInvoiceCommand, Unit>
{
    public async Task<Unit> Handle(PayInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await invoices.GetById(new InvoiceId(cmd.InvoiceId), ct)
            ?? throw new DomainException($"Invoice {cmd.InvoiceId} not found.");
        invoice.Pay(clock.UtcNow);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
