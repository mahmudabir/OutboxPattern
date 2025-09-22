using System.Threading.Tasks;

namespace OutboxPattern.Infrastructure.EventBus;

public interface IEventHandler
{
    bool CanHandle(IEvent @event);
    Task HandleAsync(IEvent @event, CancellationToken cancellationToken = default);
}

public interface IEventHandler<in TEvent> : IEventHandler where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);

    // Default interface implementations to bridge to non-generic interface
    bool IEventHandler.CanHandle(IEvent @event) => @event is TEvent;

    Task IEventHandler.HandleAsync(IEvent @event, CancellationToken cancellationToken)
        => @event is TEvent typed ? HandleAsync(typed, cancellationToken) : Task.CompletedTask;
}
