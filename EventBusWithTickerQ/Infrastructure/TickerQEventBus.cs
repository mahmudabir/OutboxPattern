using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EventBusWithTickerQ.Abstractions;
using TickerQ.Utilities;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models;
using TickerQ.Utilities.Models.Ticker;

namespace EventBusWithTickerQ.Infrastructure;

public class TickerQEventBus(
    ITimeTickerManager<TimeTicker> timeTickerManager,
    IServiceProvider serviceProvider,
    ILogger<TickerQEventBus> logger) : IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        if (@event is null)
        {
            logger.LogWarning("[EventBus] Ignored null event of type {EventType}", typeof(TEvent).Name);
            return;
        }

        // Register the event type for deserialization if not already registered
        EventTypeRegistry.Register<TEvent>();

        var publishedAt = DateTimeOffset.UtcNow;
        logger.LogInformation("[EventBus] Publishing {EventType} at {PublishedAt:o}", typeof(TEvent).Name, publishedAt);

        using var scope = serviceProvider.CreateScope();
        var handlerInstances = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        if (handlerInstances.Length == 0)
        {
            logger.LogWarning("[EventBus] No handlers registered for {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlerInstances)
        {
            var handlerType = handler.GetType();
            var handlerKey = HandlerKeyCache.Get(handlerType);

            var jobData = new EventDispatchJobData
            {
                EventJson = JsonSerializer.Serialize(@event),
                EventTypeName = typeof(TEvent).AssemblyQualifiedName!,
                HandlerKey = handlerKey,
                PublishedAt = publishedAt
            };

            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(jobData),
                ExecutionTime = DateTime.Now.AddSeconds(2),
                Function = "DispatchSingleAsync",
                Description = $"Dispatch {typeof(TEvent).Name} to {handlerType.Name}",
                Retries = 3,
                RetryIntervals = [5, 15, 30] // set in seconds
            });

            logger.LogDebug("[EventBus] Enqueued TickerQ job for {EventType} -> handler {Handler} ({Key})", typeof(TEvent).Name, handlerType.Name, handlerKey);
        }

        return;
    }
}

public class EventDispatchJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatchJob> _logger;

    public EventDispatchJob(IServiceProvider serviceProvider, ILogger<EventDispatchJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [TickerFunction("DispatchSingleAsync", TickerTaskPriority.High)]
    public async Task DispatchSingleAsync(TickerFunctionContext<EventDispatchJobData> tickerContext, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var delayMs = (startedAt - tickerContext.Request.PublishedAt).TotalMilliseconds;
        var eventTypeName = tickerContext.Request.EventTypeName;
        
        _logger.LogInformation("[EventDispatchJob] Start handler dispatch {EventType} -> key {HandlerKey} at {StartedAt:o} (delay {DelayMs}ms)", 
            eventTypeName, tickerContext.Request.HandlerKey, startedAt, delayMs);

        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Use the event type registry to deserialize without reflection
            var deserializedEvent = EventTypeRegistry.Deserialize(
                tickerContext.Request.EventTypeName, 
                tickerContext.Request.EventJson);

            if (deserializedEvent == null)
            {
                _logger.LogError("[EventDispatchJob] Could not deserialize event type {EventType}", eventTypeName);
                return;
            }

            // Get all handlers using the non-generic interface
            var allHandlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler>();
            
            IIntegrationEventHandler? targetHandler = null;
            Type? targetType = null;
            
            foreach (var handler in allHandlers)
            {
                var handlerType = handler.GetType();
                if (HandlerKeyCache.Get(handlerType) == tickerContext.Request.HandlerKey)
                {
                    targetHandler = handler;
                    targetType = handlerType;
                    break;
                }
            }

            if (targetHandler is null)
            {
                _logger.LogError("[EventDispatchJob] No registered handler matching key {HandlerKey} for {EventType}", 
                    tickerContext.Request.HandlerKey, eventTypeName);
                return;
            }

            _logger.LogDebug("[EventDispatchJob] Invoking handler {Handler} ({Key}) for {EventType}", 
                targetType!.Name, tickerContext.Request.HandlerKey, eventTypeName);

            // Use the non-generic HandleAsync method with the properly deserialized event
            await targetHandler.HandleAsync(deserializedEvent, cancellationToken);
            //_logger.LogWarning("[EventDispatchJob] Published at {PublishedAt}. Dispatched at {DispatchedAt} for {EventType}", tickerContext.Request.PublishedAt, DateTimeOffset.UtcNow, eventTypeName);

            _logger.LogInformation("[EventDispatchJob] Completed handler {Handler} ({Key}) for {EventType} (delay {DelayMs}ms)", 
                targetType.Name, tickerContext.Request.HandlerKey, eventTypeName, delayMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[EventDispatchJob] Canceled handler key {HandlerKey} for {EventType}", 
                tickerContext.Request.HandlerKey, eventTypeName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventDispatchJob] Handler key {HandlerKey} failed for {EventType}", 
                tickerContext.Request.HandlerKey, eventTypeName);
            throw;
        }
    }
}

internal static class HandlerKeyCache
{
    private static readonly ConcurrentDictionary<Type, string> _cache = new();

    public static string Get(Type type) => _cache.GetOrAdd(type, static t => CreateKey(t));

    private static string CreateKey(Type t)
    {
        var aqn = t.AssemblyQualifiedName ?? t.FullName ?? t.Name;
        var bytes = Encoding.UTF8.GetBytes(aqn);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash.AsSpan(0, 6));
    }
}

internal static class EventTypeRegistry
{
    private static readonly ConcurrentDictionary<string, Func<string, object?>> _deserializers = new();

    public static void Register<TEvent>() where TEvent : class, IIntegrationEvent
    {
        var eventTypeName = typeof(TEvent).AssemblyQualifiedName!;
        _deserializers.TryAdd(eventTypeName, json => JsonSerializer.Deserialize<TEvent>(json));
    }

    public static object? Deserialize(string eventTypeName, string json)
    {
        return _deserializers.TryGetValue(eventTypeName, out var deserializer) 
            ? deserializer(json) 
            : null;
    }
}

public class EventDispatchJobData
{
    public required string EventJson { get; set; }
    public required string EventTypeName { get; set; }
    public required string HandlerKey { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}