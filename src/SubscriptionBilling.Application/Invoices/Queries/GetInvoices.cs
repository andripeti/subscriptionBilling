using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Invoices.Queries;

public sealed record InvoiceDto(
    Guid InvoiceId,
    Guid SubscriptionId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset IssuedAt,
    DateTimeOffset? PaidAt,
    string Status);

public sealed record GetInvoicesQuery(Guid? CustomerId, Guid? SubscriptionId)
    : IQuery<IReadOnlyList<InvoiceDto>>;

public sealed class GetInvoicesHandler(IInvoiceRepository invoices)
    : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        if (query.CustomerId is null && query.SubscriptionId is null)
            throw new Domain.Common.DomainException("Provide at least one of: customerId, subscriptionId.");

        IReadOnlyList<Invoice> result = (query.CustomerId, query.SubscriptionId) switch
        {
            (null, null) => Array.Empty<Invoice>(),
            (var c, null) => await invoices.ListByCustomer(new CustomerId(c!.Value), ct),
            (null, var s) => await invoices.ListBySubscription(new SubscriptionId(s!.Value), ct),
            _ => (await invoices.ListByCustomer(new CustomerId(query.CustomerId!.Value), ct))
                    .Where(i => i.SubscriptionId.Value == query.SubscriptionId!.Value).ToList()
        };

        return result.Select(Map).ToList();
    }

    private static InvoiceDto Map(Invoice i) => new(
        i.Id.Value, i.SubscriptionId.Value, i.CustomerId.Value,
        i.Amount.Amount, i.Amount.Currency,
        i.PeriodStart, i.PeriodEnd, i.IssuedAt, i.PaidAt, i.Status.ToString());
}
