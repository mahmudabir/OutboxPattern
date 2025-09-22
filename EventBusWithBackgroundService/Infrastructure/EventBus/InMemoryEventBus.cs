using System.Collections.Concurrent;
using System.Threading.Channels;

namespace OutboxPattern.Infrastructure.EventBus;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<IEvent> _channel;
    private readonly IServiceProvider _serviceProvider;

    public InMemoryEventBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Unbounded for simplicity. Could be bounded with backpressure.
        _channel = Channel.CreateUnbounded<IEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
    {
        // Fire-and-forget: just enqueue and return
        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    internal ChannelReader<IEvent> Reader => _channel.Reader;
}
