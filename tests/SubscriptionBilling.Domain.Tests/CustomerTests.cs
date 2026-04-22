using FluentAssertions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Domain.Tests;

public class CustomerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_email_and_trims_name()
    {
        var customer = Customer.Create("  Ada Lovelace  ", Email.Create("Ada@Example.COM"), Now);

        customer.Name.Should().Be("Ada Lovelace");
        customer.Email.Value.Should().Be("ada@example.com");
        customer.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string name)
    {
        var act = () => Customer.Create(name, Email.Create("x@y.io"), Now);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Email_rejects_invalid_input(string raw)
    {
        var act = () => Email.Create(raw);
        act.Should().Throw<DomainException>();
    }
}
