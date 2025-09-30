using EventBusWithTickerQ.Abstractions;

namespace EventBusWithTickerQ.Events;

public record OrderCreateEvent(Guid OrderId, decimal Total) : IEvent;
public record OrderUpdateEvent(Guid OrderId, decimal Total) : IEvent;
