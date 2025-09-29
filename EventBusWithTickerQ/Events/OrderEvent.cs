using EventBusWithTickerQ.Abstractions;

namespace EventBusWithTickerQ.Events;

public record OrderCreateEvent(Guid OrderId, decimal Total) : IIntegrationEvent;
public record OrderUpdateEvent(Guid OrderId, decimal Total) : IIntegrationEvent;
