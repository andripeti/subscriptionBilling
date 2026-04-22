using System.Text.Json;

namespace SubscriptionBilling.Application.Abstractions;

/// <summary>
/// Carries an idempotency key alongside a command. The decorator below stores
/// the result by key so a retried command returns the original outcome.
/// </summary>
public sealed record Idempotent<TCommand, TResult>(string Key, TCommand Command) : ICommand<TResult>
    where TCommand : ICommand<TResult>;

public sealed class IdempotentCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IIdempotencyStore store) : ICommandHandler<Idempotent<TCommand, TResult>, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> Handle(Idempotent<TCommand, TResult> command, CancellationToken ct)
    {
        var existing = await store.TryGet(command.Key, ct);
        if (existing is not null)
            return JsonSerializer.Deserialize<TResult>(existing)!;

        var result = await inner.Handle(command.Command, ct);
        await store.Save(command.Key, JsonSerializer.Serialize(result), ct);
        return result;
    }
}
