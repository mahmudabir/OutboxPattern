namespace OutboxPattern.Infrastructure.EventBus;

public sealed class EventDispatcherHostedService : BackgroundService
{
    private readonly InMemoryEventBus _bus;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventBusOptions _options;
    private readonly ILogger<EventDispatcherHostedService> _logger;

    public EventDispatcherHostedService(
        InMemoryEventBus bus,
        IServiceProvider serviceProvider,
        EventBusOptions options,
        ILogger<EventDispatcherHostedService> logger)
    {
        _bus = bus;
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = _bus.Reader;
        await foreach (var evt in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handlers = scope.ServiceProvider.GetServices<IEventHandler>();
                foreach (var handler in handlers)
                {
                    if (!handler.CanHandle(evt)) continue;

                    // Fire-and-forget with retries per handler
                    _ = ExecuteWithRetriesAsync(handler, evt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event dispatch loop error for event type {EventType}", evt.GetType().Name);
            }
        }
    }

    private async Task ExecuteWithRetriesAsync(IEventHandler handler, IEvent evt, CancellationToken ct)
    {
        var attempt = 0;
        var maxRetries = Math.Max(0, _options.HandlerMaxRetries);

        while (true)
        {
            try
            {
                await handler.HandleAsync(evt, ct);
                return; // success
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Shutdown
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries)
                {
                    _logger.LogError(ex, "Handler {Handler} failed for event {EventType} after {Attempts} attempts", handler.GetType().Name, evt.GetType().Name, attempt + 1);
                    return;
                }

                var delay = ComputeDelay(attempt, _options);
                _logger.LogWarning(ex, "Handler {Handler} failed for event {EventType}. Retrying in {Delay} (attempt {Attempt}/{Total})", handler.GetType().Name, evt.GetType().Name, delay, attempt + 1, maxRetries + 1);
                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return;
                }
                attempt++;
            }
        }
    }

    private static TimeSpan ComputeDelay(int attempt, EventBusOptions options)
    {
        // exponential backoff: base * 2^attempt
        var baseMs = options.HandlerBaseDelay.TotalMilliseconds;
        var maxMs = options.HandlerMaxDelay.TotalMilliseconds;
        var exp = Math.Min(baseMs * Math.Pow(2, attempt), maxMs);

        // jitter +/- JitterFactor
        var jitter = 1.0 + ((Random.Shared.NextDouble() * 2 - 1) * options.JitterFactor);
        var ms = Math.Max(0, exp * jitter);
        return TimeSpan.FromMilliseconds(ms);
    }
}
