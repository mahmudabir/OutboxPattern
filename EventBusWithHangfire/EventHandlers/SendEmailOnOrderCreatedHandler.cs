using EventBusWithHangfire.Abstractions;
using EventBusWithHangfire.Events;

namespace EventBusWithHangfire.EventHandlers;

public sealed class SendEmailOnOrderCreatedHandler : IIntegrationEventHandler<OrderCreatedEvent>
{
    private readonly ILogger<SendEmailOnOrderCreatedHandler> _logger;

    public SendEmailOnOrderCreatedHandler(ILogger<SendEmailOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Email] Sent order confirmation for {OrderId} with total {Total}", @event.OrderId, @event.Total);
        return Task.CompletedTask;
    }
}
