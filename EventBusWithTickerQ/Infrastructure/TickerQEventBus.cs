using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EventBusWithTickerQ.Abstractions;
using TickerQ;

namespace EventBusWithTickerQ.Infrastructure;

public class TickerQEventBus : IEventBus
{
    private readonly ITickerQClient _tickerQClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TickerQEventBus> _logger;

    public TickerQEventBus(
        ITickerQClient tickerQClient,
        IServiceProvider serviceProvider,
        ILogger<TickerQEventBus> logger)
    {
        _tickerQClient = tickerQClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        if (@event is null)
        {
            _logger.LogWarning("[EventBus] Ignored null event of type {EventType}", typeof(TEvent).Name);
            return Task.CompletedTask;
        }

        var publishedAt = DateTime.UtcNow;
        _logger.LogInformation("[EventBus] Publishing {EventType} at {PublishedAt:o}", typeof(TEvent).Name, publishedAt);

        using var scope = _serviceProvider.CreateScope();
        var handlerInstances = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        if (handlerInstances.Length == 0)
        {
            _logger.LogWarning("[EventBus] No handlers registered for {EventType}", typeof(TEvent).Name);
            return Task.CompletedTask;
        }

        foreach (var handler in handlerInstances)
        {
            var handlerType = handler.GetType();
            var handlerKey = HandlerKeyCache.Get(handlerType);
            _tickerQClient.Enqueue<EventDispatchJob>(job => job.DispatchSingleAsync(@event, handlerKey, publishedAt, default));
            _logger.LogDebug("[EventBus] Enqueued TickerQ job for {EventType} -> handler {Handler} ({Key})", typeof(TEvent).Name, handlerType.Name, handlerKey);
        }

        return Task.CompletedTask;
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

    public async Task DispatchSingleAsync<TEvent>(TEvent @event, string handlerKey, DateTime publishedAt, object? cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        var startedAt = DateTime.UtcNow;
        var delayMs = (startedAt - publishedAt).TotalMilliseconds;
        _logger.LogInformation("[EventDispatchJob] Start handler dispatch {EventType} -> key {HandlerKey} at {StartedAt:o} (delay {DelayMs}ms)", typeof(TEvent).Name, handlerKey, startedAt, delayMs);

        using var scope = _serviceProvider.CreateScope();

        try
        {
            var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
            IIntegrationEventHandler<TEvent>? target = null;
            Type? targetType = null;
            foreach (var h in handlers)
            {
                var t = h.GetType();
                if (HandlerKeyCache.Get(t) == handlerKey)
                {
                    target = h;
                    targetType = t;
                    break;
                }
            }

            if (target is null)
            {
                _logger.LogError("[EventDispatchJob] No registered handler matching key {HandlerKey} for {EventType}", handlerKey, typeof(TEvent).Name);
                return;
            }

            _logger.LogDebug("[EventDispatchJob] Invoking handler {Handler} ({Key}) for {EventType}", targetType!.Name, handlerKey, typeof(TEvent).Name);
            await target.HandleAsync(@event, CancellationToken.None);
            _logger.LogInformation("[EventDispatchJob] Completed handler {Handler} ({Key}) for {EventType} (delay {DelayMs}ms)", targetType.Name, handlerKey, typeof(TEvent).Name, delayMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[EventDispatchJob] Canceled handler key {HandlerKey} for {EventType}", handlerKey, typeof(TEvent).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventDispatchJob] Handler key {HandlerKey} failed for {EventType}", handlerKey, typeof(TEvent).Name);
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
