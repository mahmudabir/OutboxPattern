using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using EventBusWithHangfire.Abstractions;
using Hangfire;

namespace EventBusWithHangfire.Infrastructure;

public class HangfireEventBus : IEventBus
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HangfireEventBus> _logger;

    public HangfireEventBus(
        IBackgroundJobClient backgroundJobClient,
        IServiceProvider serviceProvider,
        ILogger<HangfireEventBus> logger)
    {
        _backgroundJobClient = backgroundJobClient;
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

        // Fan-out: one Hangfire job per handler for isolated retry semantics
        foreach (var handler in handlerInstances)
        {
            var handlerType = handler.GetType();
            var handlerKey = HandlerKeyCache.Get(handlerType); // short, stable id
            // NOTE: we only capture the key & event, not the handler instance
            var jobId = _backgroundJobClient.Enqueue<EventDispatchJob>(job => job.DispatchSingleAsync(@event, handlerKey, publishedAt, JobCancellationToken.Null));
            _logger.LogDebug("[EventBus] Enqueued job {JobId} for {EventType} -> handler {Handler} ({Key})", jobId, typeof(TEvent).Name, handlerType.Name, handlerKey);
        }

        return Task.CompletedTask;
    }
}

[AutomaticRetry(Attempts = 5, OnAttemptsExceeded = AttemptsExceededAction.Fail, DelaysInSeconds = new[] { 1, 5, 10, 20, 30 })]
public class EventDispatchJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatchJob> _logger;

    public EventDispatchJob(IServiceProvider serviceProvider, ILogger<EventDispatchJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [Queue("events")] // single logical queue; priority controlled via server queue ordering
    public async Task DispatchSingleAsync<TEvent>(TEvent @event, string handlerKey, DateTime publishedAt, IJobCancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        var startedAt = DateTime.UtcNow;
        var delayMs = (startedAt - publishedAt).TotalMilliseconds;
        _logger.LogInformation("[EventDispatchJob] Start handler dispatch {EventType} -> key {HandlerKey} at {StartedAt:o} (delay {DelayMs}ms)", typeof(TEvent).Name, handlerKey, startedAt, delayMs);

        cancellationToken.ThrowIfCancellationRequested();
        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Resolve all handlers for this event type in this scope; pick by cached key
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
                return; // configuration drift; don't keep retrying
            }

            _logger.LogDebug("[EventDispatchJob] Invoking handler {Handler} ({Key}) for {EventType}", targetType!.Name, handlerKey, typeof(TEvent).Name);
            // We do not propagate Hangfire's internal cancellation token to user code directly; could wrap if desired.
            await target.HandleAsync(@event, CancellationToken.None);
            _logger.LogInformation("[EventDispatchJob] Completed handler {Handler} ({Key}) for {EventType} (delay {DelayMs}ms)", targetType.Name, handlerKey, typeof(TEvent).Name, delayMs);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("[EventDispatchJob] Canceled handler key {HandlerKey} for {EventType}", handlerKey, typeof(TEvent).Name);
            throw; // propagate so Hangfire records canceled state
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventDispatchJob] Handler key {HandlerKey} failed for {EventType}", handlerKey, typeof(TEvent).Name);
            throw; // trigger retry for this handler only
        }
    }

    // Backward compatibility overload (no key) - derive key and delegate to primary method
    [Queue("events")]
    public Task DispatchSingleAsync<TEvent>(TEvent @event, IIntegrationEventHandler<TEvent> handler, DateTime publishedAt, IJobCancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        var key = HandlerKeyCache.Get(handler.GetType());
        return DispatchSingleAsync(@event, key, publishedAt, cancellationToken);
    }
}

internal static class HandlerKeyCache
{
    private static readonly ConcurrentDictionary<Type, string> _cache = new();

    public static string Get(Type type) => _cache.GetOrAdd(type, static t => CreateKey(t));

    private static string CreateKey(Type t)
    {
        // Use SHA256 of AssemblyQualifiedName truncated for readability; collision probability negligible here
        var aqn = t.AssemblyQualifiedName ?? t.FullName ?? t.Name;
        var bytes = Encoding.UTF8.GetBytes(aqn);
        var hash = SHA256.HashData(bytes);
        // 12 hex chars (~48 bits) is fine; extend if you expect thousands of handlers
        return Convert.ToHexString(hash.AsSpan(0, 6));
    }
}