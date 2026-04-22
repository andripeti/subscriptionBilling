using System.Text.RegularExpressions;
using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.Customers;

public sealed partial class Email : ValueObject
{
    private static readonly Regex Pattern = EmailRegex();

    public string Value { get; private set; } = default!;

    private Email() { } // EF Core materialization
    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("Email cannot be empty.");
        var normalized = raw.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
            throw new DomainException($"'{raw}' is not a valid email.");
        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
