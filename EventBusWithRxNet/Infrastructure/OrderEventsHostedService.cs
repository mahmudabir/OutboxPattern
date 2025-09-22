namespace EventBusWithRxNet.Infrastructure
{
    public class OrderEventsHostedService : IHostedService, IDisposable
    {
        private readonly RxEventBus _eventBus;
        private IDisposable _subscription;

        public OrderEventsHostedService(IEventBus eventBus)
        {
            _eventBus = (RxEventBus)eventBus;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _subscription = _eventBus.SubscribeAllHandlers(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _subscription?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}