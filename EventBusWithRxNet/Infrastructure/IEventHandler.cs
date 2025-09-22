namespace EventBusWithRxNet.Infrastructure
{
    public interface IEventHandler<TEvent>
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}