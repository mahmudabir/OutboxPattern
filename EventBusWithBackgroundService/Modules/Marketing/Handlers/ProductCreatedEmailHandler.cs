using OutboxPattern.Infrastructure.EventBus;
using OutboxPattern.Modules.Catalog.Events;

namespace OutboxPattern.Modules.Marketing.Handlers;

public sealed class ProductCreatedEmailHandler(ILogger<ProductCreatedEmailHandler> logger) : IEventHandler<ProductCreated>
{
    private bool IsFailed = true;

    public async Task HandleAsync(ProductCreated @event, CancellationToken cancellationToken = default)
    {
        await Task.Delay(3000);

        // Mocking a failure
        if (!IsFailed)
        {
            IsFailed = true;
            throw new Exception("Failed to send email");
        }

        // Simulate sending an email or enqueueing a mail job
        logger.LogInformation("[Marketing] Email: New product created: {ProductId} - {Name}", @event.ProductId, @event.Name);
    }
}
