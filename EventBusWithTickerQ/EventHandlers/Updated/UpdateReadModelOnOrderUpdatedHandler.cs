using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.EventHandlers.Created;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Updated;

public sealed class UpdateReadModelOnOrderUpdatedHandler : IIntegrationEventHandler<OrderUpdateEvent>
{
    private readonly ILogger<UpdateReadModelOnOrderCreatedHandler> _logger;

    public UpdateReadModelOnOrderUpdatedHandler(ILogger<UpdateReadModelOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderUpdateEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ReadModel] Updated projections for {OrderId}", @event.OrderId);
        return Task.CompletedTask;
    }

    public Task HandleAsync(object @event, CancellationToken cancellationToken = default)
    {
        if (@event is OrderUpdateEvent orderCreatedEvent)
        {
            return HandleAsync(orderCreatedEvent, cancellationToken);
        }
        
        throw new ArgumentException($"Cannot handle event of type {@event?.GetType().Name ?? "null"}", nameof(@event));
    }
}
