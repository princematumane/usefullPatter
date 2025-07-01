// UnitTests/EventBusTests.cs
public class EventBusTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILogger<InMemoryEventBus>> _loggerMock = new();
    private InMemoryEventBus _eventBus;

    public EventBusTests()
    {
        _eventBus = new InMemoryEventBus(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_ShouldInvokeAllRegisteredHandlers()
    {
        // Arrange
        var handlerMock1 = new Mock<IEventHandler<TestEvent>>();
        var handlerMock2 = new Mock<IEventHandler<TestEvent>>();
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(IEventHandler<TestEvent>)))
            .Returns(handlerMock1.Object);
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(AnotherTestHandler)))
            .Returns(handlerMock2.Object);

        _eventBus.Subscribe<TestEvent, IEventHandler<TestEvent>>();
        _eventBus.Subscribe<TestEvent, AnotherTestHandler>();
        
        var testEvent = new TestEvent();

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        handlerMock1.Verify(h => h.HandleAsync(testEvent, It.IsAny<CancellationToken>()), Times.Once);
        handlerMock2.Verify(h => h.HandleAsync(testEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogError_WhenHandlerThrows()
    {
        // Arrange
        var handlerMock = new Mock<IEventHandler<TestEvent>>();
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(IEventHandler<TestEvent>)))
            .Returns(handlerMock.Object);

        _eventBus.Subscribe<TestEvent, IEventHandler<TestEvent>>();
        var testEvent = new TestEvent();

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() => _eventBus.PublishAsync(testEvent));
        
        _loggerMock.Verify(
            x => x.LogError(
                It.IsAny<Exception>(),
                It.Is<string>(m => m.Contains("Error handling event"))),
            Times.Once);
    }

    private record TestEvent : IEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public string EventType => "Test";
    }

    private class AnotherTestHandler : IEventHandler<TestEvent>
    {
        public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

// UnitTests/HandlerTests.cs
public class OrderHandlersTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ILogger<OrderCreatedHandler>> _loggerMock = new();
    private OrderCreatedHandler _handler;

    public OrderHandlersTests()
    {
        _handler = new OrderCreatedHandler(
            _inventoryServiceMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldReserveInventoryForAllItems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var items = new List<OrderItem>
        {
            new("prod1", "Product 1", 2, 10.99m),
            new("prod2", "Product 2", 1, 5.99m)
        };
        
        var @event = new OrderCreatedEvent(
            orderId,
            "customer1",
            27.97m,
            items,
            "123 Main St");

        // Act
        await _handler.HandleAsync(@event);

        // Assert
        _inventoryServiceMock.Verify(
            x => x.ReserveItemAsync("prod1", 2, orderId, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _inventoryServiceMock.Verify(
            x => x.ReserveItemAsync("prod2", 1, orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldSendConfirmationEmail()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = "customer1";
        var items = new List<OrderItem> { new("prod1", "Product 1", 1, 10.99m) };
        
        var @event = new OrderCreatedEvent(
            orderId,
            customerId,
            10.99m,
            items,
            "123 Main St");

        // Act
        await _handler.HandleAsync(@event);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendOrderConfirmationAsync(
                customerId,
                orderId,
                10.99m,
                items,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogError_WhenInventoryServiceFails()
    {
        // Arrange
        var @event = new OrderCreatedEvent(
            Guid.NewGuid(),
            "customer1",
            10.99m,
            new List<OrderItem> { new("prod1", "Product 1", 1, 10.99m) },
            "123 Main St");

        _inventoryServiceMock.Setup(x => x.ReserveItemAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Inventory service down"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.HandleAsync(@event));
        
        _loggerMock.Verify(
            x => x.LogError(
                It.IsAny<Exception>(),
                It.Is<string>(m => m.Contains("Failed to process order creation"))),
            Times.Once);
    }
}