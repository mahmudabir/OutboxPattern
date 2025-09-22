namespace EventBusWithRxNet.Infrastructure
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent @event);
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}