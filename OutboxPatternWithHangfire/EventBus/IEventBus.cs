using System.Threading.Tasks;
using OutboxPatternWithHangfire.Events;

namespace OutboxPatternWithHangfire.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : IOutboxEvent;
    }

    public interface IEventBusHandler<in TEvent> where TEvent : IOutboxEvent
    {
        Task HandleAsync(TEvent @event);
    }
}
