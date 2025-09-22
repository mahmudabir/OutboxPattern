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
        // Fire-and-forget enqueue per subscriber via Hangfire
        _backgroundJobClient.Enqueue<EventDispatchJob>(job => job.DispatchAsync(@event));
        return Task.CompletedTask;
    }
}

public class EventDispatchJob
{
    private readonly IServiceProvider _serviceProvider;

    public EventDispatchJob(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Hangfire can serialize method args; ensure it's simple/poco; records are fine
    public async Task DispatchAsync<TEvent>(TEvent @event) where TEvent : class, IIntegrationEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event);
        }
    }
}
