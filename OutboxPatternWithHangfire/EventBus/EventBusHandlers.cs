using OutboxPatternWithHangfire.Events;

namespace OutboxPatternWithHangfire.EventBus
{
    public class OrderCreatedEventHandler : IEventBusHandler<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedEventHandler> _logger;
        public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(OrderCreatedEvent @event)
        {
            await Task.Delay(5000);
            _logger.LogInformation($"[Handler] OrderCreated: {@event.OrderId}, Customer: {@event.CustomerName}, Amount: {@event.TotalAmount}");
        }
    }

    public class OrderShippedEventHandler : IEventBusHandler<OrderShippedEvent>
    {
        private readonly ILogger<OrderShippedEventHandler> _logger;
        public OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(OrderShippedEvent @event)
        {
            await Task.Delay(5000);
            _logger.LogInformation($"[Handler] OrderShipped: {@event.OrderId}, Customer: {@event.CustomerName}, ShippedAt: {@event.ShippedAt}");
        }
    }

    public class MailSendEventHandler : IEventBusHandler<MailSendEvent>
    {
        private readonly ILogger<MailSendEventHandler> _logger;
        public MailSendEventHandler(ILogger<MailSendEventHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(MailSendEvent @event)
        {
            await Task.Delay(5000);
            _logger.LogInformation($"[Handler] Sending mail to: {@event.To}, Subject: {@event.Subject}");
        }
    }

    public class InventoryUpdateEventHandler : IEventBusHandler<InventoryUpdateEvent>
    {
        private readonly ILogger<InventoryUpdateEventHandler> _logger;
        public InventoryUpdateEventHandler(ILogger<InventoryUpdateEventHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(InventoryUpdateEvent @event)
        {
            await Task.Delay(5000);
            _logger.LogInformation($"[Handler] Inventory update for Order: {@event.OrderId}, Action: {@event.Action}");
        }
    }
}
