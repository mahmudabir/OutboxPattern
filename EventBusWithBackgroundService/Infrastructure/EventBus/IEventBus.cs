using System.Threading.Tasks;

namespace OutboxPattern.Infrastructure.EventBus;

public interface IEventBus
{
    // Fire-and-forget publish - enqueue and return immediately
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
}
