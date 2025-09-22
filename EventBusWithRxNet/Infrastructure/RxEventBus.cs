using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace EventBusWithRxNet.Infrastructure
{
    public class RxEventBus : IEventBus
    {
        private readonly ISubject<object> _subject = new Subject<object>();
        private readonly IServiceProvider _serviceProvider;
        private readonly EventBusOptions _options;

        public RxEventBus(IServiceProvider serviceProvider, EventBusOptions options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
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
            disposables.Add(SubscribeHandlersForEventType<EventBusWithRxNet.Events.OrderPlacedEvent>(cancellationToken));
            disposables.Add(SubscribeHandlersForEventType<EventBusWithRxNet.Events.OrderPaidEvent>(cancellationToken));
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
                    int attempt = 0;
                    Exception lastException = null;
                    do
                    {
                        try
                        {
                            await handler.HandleAsync(evt, cancellationToken);
                            lastException = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex.InnerException ?? ex;
                            attempt++;
                            if (attempt < _options.RetryCount)
                                await Task.Delay(_options.RetryDelayMilliseconds, cancellationToken);
                        }
                    } while (attempt < _options.RetryCount && !cancellationToken.IsCancellationRequested);
                    if (lastException != null)
                    {
                        Console.WriteLine($"[EventBus] Handler failed after {_options.RetryCount} attempts: {lastException.Message}");
                    }
                }
            });
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