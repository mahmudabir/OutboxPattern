using OutboxPattern.Infrastructure.EventBus;

namespace OutboxPattern.Modules.Catalog.Events;

public sealed record ProductCreated(Guid ProductId, string Name) : IEvent;
