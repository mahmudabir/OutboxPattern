using EventBusWithQuartz.Abstractions;

namespace EventBusWithQuartz.Events;

public record OrderCreatedEvent(Guid OrderId, decimal Total) : IIntegrationEvent;
