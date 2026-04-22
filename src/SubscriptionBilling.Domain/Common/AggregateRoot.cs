namespace SubscriptionBilling.Domain.Common;

/// <summary>
/// Non-generic base for infrastructure code (e.g. the outbox interceptor) that needs
/// to collect domain events without knowing the concrete ID type.
/// </summary>
public abstract class AggregateRootBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Returns all pending events and clears the list atomically.</summary>
    public IReadOnlyList<IDomainEvent> GetAndClearEvents()
    {
        var snapshot = _domainEvents.ToList();
        _domainEvents.Clear();
        return snapshot;
    }
}

public abstract class AggregateRoot<TId> : AggregateRootBase, IEquatable<AggregateRoot<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected AggregateRoot(TId id) => Id = id;
    protected AggregateRoot() { }

    public bool Equals(AggregateRoot<TId>? other) =>
        other is not null && GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is AggregateRoot<TId> e && Equals(e);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(AggregateRoot<TId>? left, AggregateRoot<TId>? right) => Equals(left, right);
    public static bool operator !=(AggregateRoot<TId>? left, AggregateRoot<TId>? right) => !Equals(left, right);
}
