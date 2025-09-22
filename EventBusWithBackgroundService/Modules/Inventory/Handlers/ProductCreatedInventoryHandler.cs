using OutboxPattern.Infrastructure.EventBus;
using OutboxPattern.Modules.Catalog.Events;

namespace OutboxPattern.Modules.Inventory.Handlers;

public sealed class ProductCreatedInventoryHandler(ILogger<ProductCreatedInventoryHandler> logger) : IEventHandler<ProductCreated>
{
    public async Task HandleAsync(ProductCreated @event, CancellationToken cancellationToken = default)
    {
        // Simulate inventory projection update
        await Task.Delay(3000);
        logger.LogInformation("[Inventory] Reserve stock for product: {ProductId} - {Name}", @event.ProductId, @event.Name);
    }
}
