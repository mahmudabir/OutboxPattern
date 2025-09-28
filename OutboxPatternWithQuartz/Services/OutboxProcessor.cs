using Microsoft.EntityFrameworkCore;
using OutboxPatternWithQuartz.Data;
using OutboxPatternWithQuartz.EventBus;
using OutboxPatternWithQuartz.Events;
using Quartz;
using System.Text.Json;

namespace OutboxPatternWithQuartz.Services;

public class OutboxProcessor : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

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

    public async Task Execute(IJobExecutionContext context)
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
                        await eventBus.PublishAsync((dynamic)@event);
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown outbox event type: {Type}", msg.Type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {Id} of type {Type}", msg.Id, msg.Type);
            }
            msg.Processed = true;
            msg.ProcessedOn = DateTime.UtcNow;
        });

        await Task.WhenAll(tasks);
        await db.SaveChangesAsync();
    }
}
