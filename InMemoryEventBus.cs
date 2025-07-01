// Infrastructure/EventBus/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent;
    
    void Subscribe<TEvent, THandler>() 
        where TEvent : IEvent
        where THandler : IEventHandler<TEvent>;
    
    Task PublishAllAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
}

// Infrastructure/EventBus/InMemoryEventBus.cs
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly ConcurrentDictionary<Type, List<Type>> _handlers = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public InMemoryEventBus(
        IServiceProvider serviceProvider,
        ILogger<InMemoryEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        
        var eventType = typeof(TEvent);
        
        if (!_handlers.TryGetValue(eventType, out var handlerTypes))
        {
            _logger.LogDebug("No handlers registered for event {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation("Publishing event {EventId} of type {EventType}", 
            @event.EventId, eventType.Name);
        
        var exceptions = new List<Exception>();
        
        foreach (var handlerType in handlerTypes)
        {
            try
            {
                if (_serviceProvider.GetService(handlerType) is IEventHandler<TEvent> handler)
                {
                    await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling event {EventId} with handler {HandlerType}", 
                    @event.EventId, handlerType.Name);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"One or more handlers failed for event {@event.EventId}", exceptions);
        }
    }

    public void Subscribe<TEvent, THandler>() 
        where TEvent : IEvent
        where THandler : IEventHandler<TEvent>
    {
        var eventType = typeof(TEvent);
        var handlerType = typeof(THandler);

        _semaphore.Wait();
        try
        {
            var handlers = _handlers.GetOrAdd(eventType, _ => new List<Type>());
            
            if (!handlers.Contains(handlerType))
            {
                handlers.Add(handlerType);
                _logger.LogDebug("Registered handler {HandlerType} for event {EventType}", 
                    handlerType.Name, eventType.Name);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAllAsync(
        IEnumerable<IEvent> events, 
        CancellationToken cancellationToken = default)
    {
        var tasks = events.Select(e => 
        {
            var method = typeof(IEventBus).GetMethod(nameof(PublishAsync))
                ?.MakeGenericMethod(e.GetType());
            return (Task)method?.Invoke(this, new object[] { e, cancellationToken })!;
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}

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
}