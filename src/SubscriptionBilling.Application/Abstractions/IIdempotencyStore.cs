namespace SubscriptionBilling.Application.Abstractions;

/// <summary>
/// Stores a per-key result so that repeated commands with the same idempotency key
/// return the original outcome rather than re-executing.
/// </summary>
public interface IIdempotencyStore
{
    Task<string?> TryGet(string key, CancellationToken ct);
    Task Save(string key, string serializedResult, CancellationToken ct);
}
