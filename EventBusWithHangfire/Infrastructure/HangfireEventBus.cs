using EventBusWithHangfire.Abstractions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace EventBusWithHangfire.Infrastructure;

public class HangfireEventBus : IEventBus
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<HangfireEventBus> _logger;

    public HangfireEventBus(IBackgroundJobClient backgroundJobClient, ILogger<HangfireEventBus> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        var publishedAt = DateTime.UtcNow;
        var publishedTicks = publishedAt.Ticks;
        _logger.LogInformation("[EventBus] Publishing {EventType} at {PublishedAt:o} ({Ticks})", typeof(TEvent).Name, publishedAt, publishedTicks);

        // ONLY enqueue once (duplicate Create+Enqueue previously caused extra overhead)
        var jobId = _backgroundJobClient.Enqueue<EventDispatchJob>(job => job.DispatchAsync(@event, publishedAt, JobCancellationToken.Null));
        _logger.LogDebug("[EventBus] Job {JobId} enqueued for {EventType}", jobId, typeof(TEvent).Name);
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

    [Queue("events")] // ensure server options list this queue and preferably first for priority
    public async Task DispatchAsync<TEvent>(TEvent @event, DateTime publishedAt, IJobCancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        var startedAt = DateTime.UtcNow;
        var delay = (startedAt - publishedAt).TotalMilliseconds;
        _logger.LogInformation("[EventDispatchJob] Start {EventType} at {StartedAt:o}, published {PublishedAt:o}, delay {DelayMs}ms", typeof(TEvent).Name, startedAt, publishedAt, delay);

        cancellationToken.ThrowIfCancellationRequested();
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        if (handlers.Length == 0)
        {
            _logger.LogWarning("[EventDispatchJob] No handlers for {EventType}", typeof(TEvent).Name);
            return;
        }

        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                _logger.LogDebug("[EventDispatchJob] Invoking handler {Handler} for {EventType}", handler.GetType().Name, typeof(TEvent).Name);
                await handler.HandleAsync(@event, default); // pass default token
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[EventDispatchJob] Canceled handler {Handler}", handler.GetType().Name);
                throw; // propagate cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EventDispatchJob] Handler {Handler} failed for {EventType}", handler.GetType().Name, typeof(TEvent).Name);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions); // trigger retry
        }

        _logger.LogInformation("[EventDispatchJob] Completed {EventType} ({HandlerCount} handlers) total delay {DelayMs}ms", typeof(TEvent).Name, handlers.Length, delay);
    }
}
