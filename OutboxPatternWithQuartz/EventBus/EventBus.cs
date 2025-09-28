using Microsoft.Extensions.DependencyInjection;
using OutboxPatternWithQuartz.Events;

namespace OutboxPatternWithQuartz.EventBus;

public class EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger) : IEventBus
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<EventBus> _logger = logger;

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IOutboxEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventBusHandler<TEvent>>().ToList();
        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers registered for event type {EventType}", typeof(TEvent).Name);
            return;
        }
        var tasks = handlers.Select(h => h.HandleAsync(@event));
        await Task.WhenAll(tasks);
    }
}
