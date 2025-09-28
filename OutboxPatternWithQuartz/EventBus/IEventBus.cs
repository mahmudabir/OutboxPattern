using OutboxPatternWithQuartz.Events;

namespace OutboxPatternWithQuartz.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IOutboxEvent;
}

public interface IEventBusHandler<in TEvent> where TEvent : IOutboxEvent
{
    Task HandleAsync(TEvent @event);
}
