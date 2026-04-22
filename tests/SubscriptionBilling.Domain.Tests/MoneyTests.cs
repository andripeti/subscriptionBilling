using FluentAssertions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Shared;

namespace SubscriptionBilling.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_rejects_negative_amount()
    {
        var act = () => Money.Of(-1m, "EUR");
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Of_rejects_invalid_currency(string currency)
    {
        var act = () => Money.Of(1m, currency);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Add_throws_on_currency_mismatch()
    {
        var a = Money.Of(1m, "EUR");
        var b = Money.Of(1m, "USD");

        var act = () => a.Add(b);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_is_value_based()
    {
        Money.Of(1.50m, "eur").Should().Be(Money.Of(1.5m, "EUR"));
    }
}
