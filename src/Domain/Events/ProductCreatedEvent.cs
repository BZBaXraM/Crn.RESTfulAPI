namespace Domain.Events;

public sealed record ProductCreatedEvent(int ProductId, string ProductName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
