using EventBusWithTickerQ.Abstractions;

namespace EventBusWithTickerQ.Events;

public record OrderCreatedEvent(Guid OrderId, decimal Total) : IIntegrationEvent;
