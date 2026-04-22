using FluentAssertions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Invoices.Events;
using SubscriptionBilling.Domain.Shared;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Domain.Tests;

public class InvoiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

    private static Invoice NewInvoice() =>
        Subscription.Create(
            CustomerId.New(),
            Plan.Create("Starter", Money.Of(5m, "USD"), BillingCycle.Monthly),
            Now).Activate(Now);

    [Fact]
    public void Pay_marks_invoice_paid_and_emits_PaymentReceived()
    {
        var invoice = NewInvoice();
        invoice.ClearDomainEvents();

        var paidAt = Now.AddDays(1);
        invoice.Pay(paidAt);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().Be(paidAt);
        invoice.DomainEvents.Should().ContainSingle(e => e is PaymentReceived);
    }

    [Fact]
    public void Pay_twice_throws()
    {
        var invoice = NewInvoice();
        invoice.Pay(Now.AddDays(1));

        var act = () => invoice.Pay(Now.AddDays(2));

        act.Should().Throw<DomainException>().WithMessage("*already paid*");
    }
}
