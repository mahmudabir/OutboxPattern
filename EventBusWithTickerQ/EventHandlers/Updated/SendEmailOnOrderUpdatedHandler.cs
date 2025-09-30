using EventBusWithTickerQ.Abstractions;
using EventBusWithTickerQ.EventHandlers.Created;
using EventBusWithTickerQ.Events;

namespace EventBusWithTickerQ.EventHandlers.Updated;

public sealed class SendEmailOnOrderUpdatedHandler : IEventHandler<OrderUpdateEvent>
{
    private readonly ILogger<SendEmailOnOrderCreatedHandler> _logger;
    private static bool IsFailed = true;

    public SendEmailOnOrderUpdatedHandler(ILogger<SendEmailOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderUpdateEvent @event, CancellationToken cancellationToken = default)
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
}
