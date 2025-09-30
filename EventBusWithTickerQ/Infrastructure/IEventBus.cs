using EventBusWithTickerQ.Abstractions;

namespace EventBusWithTickerQ.Infrastructure;

public interface IEventBus
{
    // Fire-and-forget publish: enqueue background job to process subscribers
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent;
}
