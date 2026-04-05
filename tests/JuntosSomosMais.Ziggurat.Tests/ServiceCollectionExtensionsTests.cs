using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JuntosSomosMais.Ziggurat.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddConsumerService_ShouldRegisterPipelineAsConsumerService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConsumerService<TestMessage, TestConsumerService>(options => { });
        var provider = services.BuildServiceProvider();

        // Act
        var consumer = provider.GetRequiredService<IConsumerService<TestMessage>>();
        await consumer.ProcessMessageAsync(new TestMessage { MessageId = "1", MessageGroup = "test" }, TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<PipelineHandler<TestMessage>>(consumer);
    }

    [Fact]
    public void AddConsumerService_ShouldRegisterConcreteService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddConsumerService<TestMessage, TestConsumerService>(options => { });
        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetRequiredService<TestConsumerService>();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void AddConsumerService_ShouldInvokeSetupActionExtensions()
    {
        // Arrange
        var extensionInvoked = false;
        var services = new ServiceCollection();

        // Act
        services.AddConsumerService<TestMessage, TestConsumerService>(options =>
        {
            options.Use<TestMiddleware>();
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var middlewares = provider.GetServices<IConsumerMiddleware<TestMessage>>();
        Assert.Single(middlewares);
        Assert.IsType<TestMiddleware>(middlewares.First());
    }

    [Fact]
    public void AddConsumerService_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddConsumerService<TestMessage, TestConsumerService>(options => { });

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public async Task AddConsumerService_WithMiddleware_ShouldExecuteMiddlewareThenService()
    {
        // Arrange
        var executionOrder = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(executionOrder);
        services.AddConsumerService<TestMessage, TrackingConsumerService>(options =>
        {
            options.Use<TrackingMiddleware>();
        });
        var provider = services.BuildServiceProvider();

        // Act
        var consumer = provider.GetRequiredService<IConsumerService<TestMessage>>();
        await consumer.ProcessMessageAsync(new TestMessage { MessageId = "1", MessageGroup = "test" }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new List<string> { "TrackingMiddleware", "TrackingConsumerService" }, executionOrder);
    }

    public class TestMessage : IMessage
    {
        public string MessageId { get; set; }
        public string MessageGroup { get; set; }
    }

    public class TestConsumerService : IConsumerService<TestMessage>
    {
        public Task ProcessMessageAsync(TestMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class TestMiddleware : IConsumerMiddleware<TestMessage>
    {
        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            await next(message, cancellationToken);
        }
    }

    public class TrackingConsumerService : IConsumerService<TestMessage>
    {
        private readonly List<string> _order;

        public TrackingConsumerService(List<string> order)
        {
            _order = order;
        }

        public Task ProcessMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            _order.Add("TrackingConsumerService");
            return Task.CompletedTask;
        }
    }

    public class TrackingMiddleware : IConsumerMiddleware<TestMessage>
    {
        private readonly List<string> _order;

        public TrackingMiddleware(List<string> order)
        {
            _order = order;
        }

        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            _order.Add("TrackingMiddleware");
            await next(message, cancellationToken);
        }
    }
}
