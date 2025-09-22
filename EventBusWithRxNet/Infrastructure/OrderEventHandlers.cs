using EventBusWithRxNet.Events;

namespace EventBusWithRxNet.Infrastructure
{
    public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IEventHandler<OrderPlacedEvent>
    {
        public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(3000);
            logger.LogInformation($"[Handler] OrderPlacedEvent: OrderId={@event.OrderId}, UserId={@event.UserId}");
        }
    }

    public class EmailAfterOrderPlacedHandler(ILogger<EmailAfterOrderPlacedHandler> logger) : IEventHandler<OrderPlacedEvent>
    {
        private bool IsFailed = true;

        public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(3000);

            // Mocking a failure
            if (!IsFailed)
            {
                IsFailed = true;
                throw new Exception("Failed to send email");
            }
            logger.LogInformation($"[Handler] EmailAfterOrderPlacedHandler: OrderId={@event.OrderId}, UserId={@event.UserId}");
        }
    }

    public class OrderPaidHandler(ILogger<OrderPaidHandler> logger) : IEventHandler<OrderPaidEvent>
    {
        public async Task HandleAsync(OrderPaidEvent @event, CancellationToken cancellationToken = default)
        {
            await Task.Delay(3000);
            logger.LogInformation($"[Handler] OrderPaidEvent: OrderId={@event.OrderId}, PaidAmount={@event.PaidAmount}");
        }
    }
}