using EventBusWithHangfire.Abstractions;
using EventBusWithHangfire.Events;

namespace EventBusWithHangfire.EventHandlers;

public sealed class UpdateReadModelOnOrderCreatedHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<UpdateReadModelOnOrderCreatedHandler> _logger;

    public UpdateReadModelOnOrderCreatedHandler(ILogger<UpdateReadModelOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ReadModel] Updated projections for {OrderId}", @event.OrderId);
        return Task.CompletedTask;
    }
}
