using System.Text.Json;
using System.Text.Json.Serialization;
using SubscriptionBilling.Domain.Shared;

namespace SubscriptionBilling.Infrastructure.Outbox;

/// <summary>
/// Shared JsonSerializerOptions used by both the outbox interceptor (serialize)
/// and the outbox processor (deserialize). Keeps round-trip behaviour consistent.
/// </summary>
internal static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        Converters = { new MoneyJsonConverter() }
    };
}

internal sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var amount = doc.RootElement.GetProperty("Amount").GetDecimal();
        var currency = doc.RootElement.GetProperty("Currency").GetString()!;
        return Money.Of(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Amount", value.Amount);
        writer.WriteString("Currency", value.Currency);
        writer.WriteEndObject();
    }
}
