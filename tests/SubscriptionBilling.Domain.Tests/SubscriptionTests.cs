using FluentAssertions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Shared;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.Subscriptions.Events;

namespace SubscriptionBilling.Domain.Tests;

public class SubscriptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);

    private static Subscription NewMonthlySubscription() =>
        Subscription.Create(
            CustomerId.New(),
            Plan.Create("Pro", Money.Of(9.99m, "EUR"), BillingCycle.Monthly),
            Now);

    [Fact]
    public void Activate_generates_first_invoice_and_emits_SubscriptionActivated()
    {
        var sub = NewMonthlySubscription();

        var invoice = sub.Activate(Now);

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.ActivatedAt.Should().Be(Now);
        sub.NextBillingDate.Should().Be(Now.AddMonths(1));
        invoice.Status.Should().Be(InvoiceStatus.Pending);
        invoice.Amount.Should().Be(Money.Of(9.99m, "EUR"));
        invoice.PeriodStart.Should().Be(Now);
        invoice.PeriodEnd.Should().Be(Now.AddMonths(1));

        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivated);
    }

    [Fact]
    public void Activating_twice_throws()
    {
        var sub = NewMonthlySubscription();
        sub.Activate(Now);

        var act = () => sub.Activate(Now);

        act.Should().Throw<DomainException>().WithMessage("*already active*");
    }

    [Fact]
    public void Activating_a_cancelled_subscription_throws()
    {
        var sub = NewMonthlySubscription();
        sub.Activate(Now);
        sub.Cancel(Now.AddDays(1));

        var act = () => sub.Activate(Now.AddDays(2));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GenerateNextInvoice_returns_null_before_billing_date()
    {
        var sub = NewMonthlySubscription();
        sub.Activate(Now);

        var next = sub.GenerateNextInvoice(Now.AddDays(15));

        next.Should().BeNull();
    }

    [Fact]
    public void GenerateNextInvoice_emits_a_new_invoice_when_period_elapses()
    {
        var sub = NewMonthlySubscription();
        sub.Activate(Now);

        var dueAt = Now.AddMonths(1);
        var next = sub.GenerateNextInvoice(dueAt);

        next.Should().NotBeNull();
        next!.PeriodStart.Should().Be(dueAt);
        next.PeriodEnd.Should().Be(dueAt.AddMonths(1));
        sub.NextBillingDate.Should().Be(dueAt.AddMonths(1));
    }

    [Fact]
    public void Cancel_stops_future_invoices_but_keeps_history()
    {
        var sub = NewMonthlySubscription();
        var first = sub.Activate(Now);

        sub.Cancel(Now.AddDays(5));

        sub.Status.Should().Be(SubscriptionStatus.Cancelled);
        sub.NextBillingDate.Should().BeNull();
        sub.GenerateNextInvoice(Now.AddMonths(2)).Should().BeNull();
        first.Should().NotBeNull(); // historical invoice still exists
    }

    [Fact]
    public void Cancel_twice_throws()
    {
        var sub = NewMonthlySubscription();
        sub.Activate(Now);
        sub.Cancel(Now.AddDays(1));

        var act = () => sub.Cancel(Now.AddDays(2));

        act.Should().Throw<DomainException>();
    }
}
