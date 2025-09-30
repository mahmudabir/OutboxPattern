using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.EventHandlers.Created;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Updated;

public sealed class UpdateReadModelOnOrderUpdatedHandler : IEventHandler<OrderUpdateEvent>
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
}
