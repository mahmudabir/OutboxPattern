using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Created;

public sealed class UpdateReadModelOnOrderCreatedHandler : IEventHandler<OrderCreateEvent>
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
}
