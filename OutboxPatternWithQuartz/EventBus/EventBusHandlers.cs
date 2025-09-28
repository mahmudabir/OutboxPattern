using OutboxPatternWithQuartz.Events;

namespace OutboxPatternWithQuartz.EventBus;

public class OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger) : IEventBusHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger = logger;
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        await Task.Delay(3000);
        _logger.LogInformation("[Handler] OrderCreated: {OrderId}, Customer: {CustomerName}, Amount: {Amount}", @event.OrderId, @event.CustomerName, @event.TotalAmount);
    }
}

public class OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger) : IEventBusHandler<OrderShippedEvent>
{
    private readonly ILogger<OrderShippedEventHandler> _logger = logger;
    public async Task HandleAsync(OrderShippedEvent @event)
    {
        await Task.Delay(3000);
        _logger.LogInformation("[Handler] OrderShipped: {OrderId}, Customer: {CustomerName}, ShippedAt: {ShippedAt}", @event.OrderId, @event.CustomerName, @event.ShippedAt);
    }
}

public class MailSendEventHandler(ILogger<MailSendEventHandler> logger) : IEventBusHandler<MailSendEvent>
{
    private readonly ILogger<MailSendEventHandler> _logger = logger;
    public async Task HandleAsync(MailSendEvent @event)
    {
        await Task.Delay(3000);
        _logger.LogInformation("[Handler] Sending mail to: {To}, Subject: {Subject}", @event.To, @event.Subject);
    }
}

public class InventoryUpdateEventHandler(ILogger<InventoryUpdateEventHandler> logger) : IEventBusHandler<InventoryUpdateEvent>
{
    private readonly ILogger<InventoryUpdateEventHandler> _logger = logger;
    public async Task HandleAsync(InventoryUpdateEvent @event)
    {
        await Task.Delay(3000);
        _logger.LogInformation("[Handler] Inventory update for Order: {OrderId}, Action: {Action}", @event.OrderId, @event.Action);
    }
}
