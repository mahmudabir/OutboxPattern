using System.Text.Json;
using EventBusWithHangfire.Abstractions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace EventBusWithHangfire.Infrastructure;

public class HangfireEventBus : IEventBus
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireEventBus(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        // Fire-and-forget enqueue via Hangfire (events queue)
        _backgroundJobClient.Create(
            Hangfire.Common.Job.FromExpression<EventDispatchJob>(job => job.DispatchAsync(@event, JobCancellationToken.Null)),
            new Hangfire.States.EnqueuedState("events"));
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

    // Hangfire serializes args; keep events as simple POCOs/records
    [Queue("events")]
    public async Task DispatchAsync<TEvent>(TEvent @event, IJobCancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        if (handlers.Length == 0)
        {
            _logger.LogWarning("No handlers registered for event type {EventType}", typeof(TEvent).Name);
            return;
        }

        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                _logger.LogInformation("Dispatching {EventType} to handler {Handler}", typeof(TEvent).Name, handler.GetType().Name);
                await handler.HandleAsync(@event, default);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Dispatch canceled for handler {Handler}", handler.GetType().Name);
                throw; // surface cancellation to Hangfire
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for {EventType}", handler.GetType().Name, typeof(TEvent).Name);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            // Aggregate exceptions to trigger Hangfire retry
            throw new AggregateException(exceptions);
        }

        _logger.LogInformation("Successfully dispatched {EventType} to {Count} handlers", typeof(TEvent).Name, handlers.Length);
    }
}
