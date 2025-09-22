using Hangfire;
using Microsoft.EntityFrameworkCore;
using OutboxPatternWithHangfire.Data;
using OutboxPatternWithHangfire.EventBus;
using OutboxPatternWithHangfire.Events;
using System.Text.Json;

namespace OutboxPatternWithHangfire.Services
{
    public class OutboxProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        // Type map for event type resolution
        private static readonly Dictionary<string, Type> EventTypeMap = new()
        {
            { "OrderCreated", typeof(OrderCreatedEvent) },
            { "OrderShipped", typeof(OrderShippedEvent) },
            { "MailSend", typeof(MailSendEvent) },
            { "InventoryUpdate", typeof(InventoryUpdateEvent) }
        };
        public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessOutboxAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            var messages = await db.OutboxMessages.Where(x => !x.Processed).OrderBy(x => x.OccurredOn).ToListAsync();

            var tasks = messages.Select(async msg =>
            {
                try
                {
                    if (EventTypeMap.TryGetValue(msg.Type, out var eventType))
                    {
                        var @event = (IOutboxEvent?)JsonSerializer.Deserialize(msg.Content, eventType);
                        if (@event != null)
                        {
                            await ((IEventBus)eventBus).PublishAsync((dynamic)@event);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Unknown outbox event type: {msg.Type}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process outbox message {msg.Id} of type {msg.Type}");
                }
                // Mark as processed
                msg.Processed = true;
                msg.ProcessedOn = DateTime.UtcNow;
            });

            await Task.WhenAll(tasks);
            await db.SaveChangesAsync();
        }
    }
}