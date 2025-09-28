using EventBusWithQuartz.Abstractions;

namespace EventBusWithQuartz.Infrastructure;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}
