using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Created;

public sealed class UpdateReadModelOnOrderCreatedHandler : IIntegrationEventHandler<OrderCreateEvent>
{
    private readonly ILogger<UpdateReadModelOnOrderCreatedHandler> _logger;

    public UpdateReadModelOnOrderCreatedHandler(ILogger<UpdateReadModelOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreateEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ReadModel] Updated projections for {OrderId}", @event.OrderId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(object @event, CancellationToken cancellationToken = default)
    {
        if (@event is OrderCreateEvent orderCreatedEvent)
        {
            return HandleAsync(orderCreatedEvent, cancellationToken);
        }
        
        throw new ArgumentException($"Cannot handle event of type {@event?.GetType().Name ?? "null"}", nameof(@event));
    }
}
