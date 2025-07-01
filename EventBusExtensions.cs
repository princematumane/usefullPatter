// Infrastructure/EventBus/EventBusExtensions.cs
public static class EventBusExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        return services;
    }

    public static IApplicationBuilder SubscribeEvents(this IApplicationBuilder app)
    {
        var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
        
        // Auto-discover and register all event handlers
        var handlerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                          i.GetGenericTypeDefinition() == typeof(IEventHandler<>)));

        foreach (var handlerType in handlerTypes)
        {
            var eventType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .GetGenericArguments()[0];

            var method = typeof(IEventBus).GetMethod(nameof(IEventBus.Subscribe))
                ?.MakeGenericMethod(eventType, handlerType);
            
            method?.Invoke(eventBus, Array.Empty<object>());
        }

        return app;
    }

    public static IServiceCollection AddEventHandlers(this IServiceCollection services)
{
    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !a.FullName!.StartsWith("System") && !a.FullName.StartsWith("Microsoft"));
    
    foreach (var assembly in assemblies)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                       t.GetInterfaces().Any(i => 
                           i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IEventHandler<>)));

        foreach (var type in handlerTypes)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, type);
            }
        }
    }

    return services;
}
}