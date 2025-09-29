using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Created;

public sealed class SendEmailOnOrderCreatedHandler : IIntegrationEventHandler<OrderCreateEvent>
{
    private readonly ILogger<SendEmailOnOrderCreatedHandler> _logger;
    private static bool IsFailed = true;

    public SendEmailOnOrderCreatedHandler(ILogger<SendEmailOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreateEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Email] Sent order confirmation for {OrderId} with total {Total}", @event.OrderId, @event.Total);

        if (!IsFailed)
        {
            IsFailed = true;
            _logger.LogInformation("[Email] Sending failing forcefully");
            throw new Exception("Failed to send email");
        }

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
