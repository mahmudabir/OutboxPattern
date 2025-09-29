namespace EventBusWithTickerQ.Abstractions;

public interface IIntegrationEventHandler
{
    Task HandleAsync(object @event, CancellationToken cancellationToken = default);
}

public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
