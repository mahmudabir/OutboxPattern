using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutboxPatternWithHangfire.Events;

namespace OutboxPatternWithHangfire.EventBus
{
    public class EventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBus> _logger;
        public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IOutboxEvent
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IEventBusHandler<TEvent>>().ToList();
            if (!handlers.Any())
            {
                _logger.LogWarning($"No handlers registered for event type {typeof(TEvent).Name}");
                return;
            }
            var tasks = handlers.Select(h => h.HandleAsync(@event));
            await Task.WhenAll(tasks);
        }
    }
}
