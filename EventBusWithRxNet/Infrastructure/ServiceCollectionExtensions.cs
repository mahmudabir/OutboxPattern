using Microsoft.Extensions.DependencyInjection;
using EventBusWithRxNet.Events;
using System;

namespace EventBusWithRxNet.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRxEventBus(this IServiceCollection services, Action<EventBusOptions> configureOptions = null)
        {
            var options = new EventBusOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.AddSingleton<IEventBus, RxEventBus>(sp => new RxEventBus(sp, sp.GetRequiredService<EventBusOptions>()));
            return services;
        }
        public static IServiceCollection AddEventHandlers(this IServiceCollection services)
        {
            services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedHandler>();
            services.AddScoped<IEventHandler<OrderPlacedEvent>, EmailAfterOrderPlacedHandler>();
            services.AddScoped<IEventHandler<OrderPaidEvent>, OrderPaidHandler>();
            return services;
        }
    }
}