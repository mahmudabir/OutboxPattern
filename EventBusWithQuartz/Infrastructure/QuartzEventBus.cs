using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EventBusWithQuartz.Abstractions;
using Quartz;

namespace EventBusWithQuartz.Infrastructure;

public class QuartzEventBus : IEventBus
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QuartzEventBus> _logger;

    public QuartzEventBus(
        ISchedulerFactory schedulerFactory,
        IServiceProvider serviceProvider,
        ILogger<QuartzEventBus> logger)
    {
        _schedulerFactory = schedulerFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class, IIntegrationEvent
    {
        if (@event is null)
        {
            _logger.LogWarning("[EventBus] Ignored null event of type {EventType}", typeof(TEvent).Name);
            return;
        }

        var publishedAt = DateTime.UtcNow;
        _logger.LogInformation("[EventBus] Publishing {EventType} at {PublishedAt:o}", typeof(TEvent).Name, publishedAt);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();
        if (handlers.Length == 0)
        {
            _logger.LogWarning("[EventBus] No handlers registered for {EventType}", typeof(TEvent).Name);
            return;
        }

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var payload = JsonSerializer.Serialize(@event, @event.GetType());

        foreach (var h in handlers)
        {
            var handlerType = h.GetType();
            var handlerKey = HandlerKeyCache.Get(handlerType);

            var jobKey = new JobKey($"evt-{typeof(TEvent).Name}-{handlerKey}-{Guid.NewGuid():N}", "events");
            var jobData = new JobDataMap
            {
                [EventDispatchJob<TEvent>.PayloadKey] = payload,
                [EventDispatchJob<TEvent>.HandlerKey] = handlerKey,
                [EventDispatchJob<TEvent>.PublishedAtKey] = publishedAt.ToString("O"),
                // Store retry as string because UseProperties=true requires string values
                [EventDispatchJob<TEvent>.RetryCountKey] = 0.ToString(CultureInfo.InvariantCulture)
            };

            var job = JobBuilder.Create<EventDispatchJob<TEvent>>()
                .WithIdentity(jobKey)
                .UsingJobData(jobData)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"trg-{jobKey.Name}", "events")
                .ForJob(job)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
            _logger.LogDebug("[EventBus] Scheduled Quartz job {Job} for {EventType} -> handler {Handler} ({Key})", jobKey, typeof(TEvent).Name, handlerType.Name, handlerKey);
        }
    }
}

// Generic job per event type (mirrors Hangfire generic dispatch method pattern)
public class EventDispatchJob<TEvent> : IJob where TEvent : class, IIntegrationEvent
{
    public const string PayloadKey = "payload";
    public const string HandlerKey = "handlerKey";
    public const string PublishedAtKey = "publishedAt";
    public const string RetryCountKey = "retry";

    private static readonly int[] RetryDelaysSeconds = { 1, 5, 10, 20, 30 };

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventDispatchJob<TEvent>> _logger;

    public EventDispatchJob(IServiceProvider serviceProvider, ILogger<EventDispatchJob<TEvent>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await Task.Delay(1000);
        var data = context.MergedJobDataMap;
        var payload = data.GetString(PayloadKey)!;
        var handlerKey = data.GetString(HandlerKey)!;
        var publishedAtString = data.GetString(PublishedAtKey)!;
        var retry = data.GetInt(RetryCountKey); // works even if stored as string

        var publishedAt = DateTime.Parse(publishedAtString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var startedAt = DateTime.UtcNow;
        var delayMs = (startedAt - publishedAt).TotalMilliseconds;
        _logger.LogInformation("[EventDispatchJob] Start handler dispatch {EventType} -> key {HandlerKey} at {StartedAt:o} (delay {Delay}ms, retry {Retry})", typeof(TEvent).Name, handlerKey, startedAt, delayMs, retry);

        TEvent? evt = default;
        try
        {
            evt = JsonSerializer.Deserialize<TEvent>(payload);
            if (evt == null)
            {
                _logger.LogError("[EventDispatchJob] Deserialization returned null for {EventType}", typeof(TEvent).Name);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventDispatchJob] Failed to deserialize payload for {EventType}");
            return; // not retrying malformed payload
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
            IIntegrationEventHandler<TEvent>? target = null;
            foreach (var h in handlers)
            {
                if (HandlerKeyCache.Get(h.GetType()) == handlerKey)
                {
                    target = h; break;
                }
            }

            if (target is null)
            {
                _logger.LogError("[EventDispatchJob] No registered handler matching key {HandlerKey} for {EventType}", handlerKey, typeof(TEvent).Name);
                return; // configuration drift; do not retry
            }

            await target.HandleAsync(evt, CancellationToken.None);
            _logger.LogInformation("[EventDispatchJob] Completed handler {HandlerKey} for {EventType}", handlerKey, typeof(TEvent).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventDispatchJob] Handler {HandlerKey} failed for {EventType} (retry {Retry})", handlerKey, typeof(TEvent).Name, retry);
            await ScheduleRetryAsync(context, payload, handlerKey, publishedAtString, retry);
        }
    }

    private async Task ScheduleRetryAsync(IJobExecutionContext ctx, string payload, string handlerKey, string publishedAt, int currentRetry)
    {
        if (currentRetry >= RetryDelaysSeconds.Length)
            return;

        var nextRetry = currentRetry + 1;
        var delay = RetryDelaysSeconds[currentRetry];

        var jobKey = new JobKey($"evt-retry-{typeof(TEvent).Name}-{handlerKey}-{Guid.NewGuid():N}", "events");
        var jobData = new JobDataMap
        {
            [PayloadKey] = payload,
            [HandlerKey] = handlerKey,
            [PublishedAtKey] = publishedAt,
            [RetryCountKey] = nextRetry.ToString(CultureInfo.InvariantCulture)
        };

        var job = JobBuilder.Create<EventDispatchJob<TEvent>>()
            .WithIdentity(jobKey)
            .UsingJobData(jobData)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg-{jobKey.Name}", "events")
            .StartAt(DateTimeOffset.UtcNow.AddSeconds(delay))
            .ForJob(job)
            .Build();

        await ctx.Scheduler.ScheduleJob(job, trigger);
        _logger.LogWarning("[EventDispatchJob] Scheduled retry {Retry} in {Delay}s for handler {HandlerKey} {EventType}", nextRetry, delay, handlerKey, typeof(TEvent).Name);
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
