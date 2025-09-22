using System.Reflection;

namespace OutboxPattern.Infrastructure.EventBus;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, Action<EventBusOptions>? configure = null)
    {
        services.AddSingleton<InMemoryEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
        services.AddHostedService<EventDispatcherHostedService>();

        var options = new EventBusOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // Register all IEventHandler<T> implementations from application assemblies
        RegisterHandlersFromAssembly(services, Assembly.GetExecutingAssembly());

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var marker = typeof(IEventHandler);
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && marker.IsAssignableFrom(t));

        foreach (var implType in types)
        {
            // Register as non-generic IEventHandler to allow simple resolution
            services.AddScoped(typeof(IEventHandler), implType);

            // Also register its generic interfaces for flexibility
            var genericInterfaces = implType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));
            foreach (var gi in genericInterfaces)
            {
                services.AddScoped(gi, implType);
            }
        }
    }
}
