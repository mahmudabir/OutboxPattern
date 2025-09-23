using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EventBusWithRxNet.Infrastructure
{
    public class RxEventBus : IEventBus
    {
        private readonly ISubject<object> _subject = new Subject<object>();
        private readonly IServiceProvider _serviceProvider;
        private readonly EventBusOptions _options;
        private readonly ILogger<RxEventBus> _logger;

        public RxEventBus(IServiceProvider serviceProvider, EventBusOptions options, ILogger<RxEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _logger = logger;
        }

        public void Publish<TEvent>(TEvent @event)
        {
            _subject.OnNext(@event);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            return _subject.AsObservable().OfType<TEvent>().Subscribe(handler);
        }

        public IDisposable SubscribeAllHandlers(CancellationToken cancellationToken = default)
        {
            // Subscribe to all events and dispatch to handlers without reflection
            var disposables = new List<IDisposable>();
            // For each event type, subscribe and dispatch to handlers
            // This requires knowing all event types at startup
            // We'll subscribe for each known event type
            disposables.Add(SubscribeHandlersForEventType<Events.OrderPlacedEvent>(cancellationToken));
            disposables.Add(SubscribeHandlersForEventType<Events.OrderPaidEvent>(cancellationToken));
            // Add more event types here as needed
            return new CompositeDisposable(disposables);
        }

        private IDisposable SubscribeHandlersForEventType<TEvent>(CancellationToken cancellationToken)
        {
            return _subject.AsObservable().OfType<TEvent>().Subscribe(async evt =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();

                foreach (var handler in handlers)
                {
                    // Fire-and-forget with retries per handler
                    _ = ExecuteWithRetriesAsync(handler, evt, cancellationToken);
                }
            });
        }

        private async Task ExecuteWithRetriesAsync<TEvent>(IEventHandler<TEvent> handler, TEvent evt, CancellationToken ct)
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

        private class CompositeDisposable : IDisposable
        {
            private readonly IEnumerable<IDisposable> _disposables;
            public CompositeDisposable(IEnumerable<IDisposable> disposables) => _disposables = disposables;
            public void Dispose()
            {
                foreach (var d in _disposables)
                    d.Dispose();
            }
        }
    }
}