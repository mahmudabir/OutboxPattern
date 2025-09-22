using EventBusWithHangfire.Abstractions;

namespace EventBusWithHangfire.Events;

public record OrderCreatedEvent(Guid OrderId, decimal Total) : IIntegrationEvent;
