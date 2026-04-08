using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using JuntosSomosMais.Ziggurat.CapAdapter.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace JuntosSomosMais.Ziggurat.CapAdapter.Tests;

public class TopicConsumerDiscoveryTests
{
    [Fact]
    public void BuildTopicConsumerMap_WithValidConsumers_ShouldDiscoverAllTopics()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Assert
        Assert.Contains("test.topic.created", map.Keys);
        Assert.Contains("another.topic.created", map.Keys);
    }

    [Fact]
    public void BuildTopicConsumerMap_WithValidConsumers_ShouldMapCorrectConsumerNames()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Assert
        Assert.Equal(nameof(ValidConsumer), map["test.topic.created"].ConsumerName);
        Assert.Equal(nameof(AnotherValidConsumer), map["another.topic.created"].ConsumerName);
    }

    [Fact]
    public void BuildTopicConsumerMap_SubscriberWithoutCapSubscribeAttribute_ShouldNotBeDiscovered()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Assert
        Assert.DoesNotContain(map.Values, v => v.ConsumerName == nameof(ConsumerWithoutAttribute));
    }

    [Fact]
    public void BuildTopicConsumerMap_HandlerParentNotImplementingICapSubscribe_ShouldNotBeDiscovered()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Assert
        Assert.DoesNotContain(map.Values, v => v.ConsumerName == nameof(NotASubscriber));
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenRegisterInvoked_ShouldRegisterSubscriberAsScoped()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act
        map["test.topic.created"].Register(services);

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ValidConsumer));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenRegisterInvoked_ShouldRegisterHandlerAsConsumerService()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act
        map["test.topic.created"].Register(services);

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ValidConsumer.Handler));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenRegisterInvoked_ShouldRegisterPipelineHandlerAsConsumerServiceInterface()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act
        map["test.topic.created"].Register(services);

        // Assert
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IConsumerService<TestMessage>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenRegisterInvoked_ShouldCallConfiguratorWithCorrectMessageType()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act
        map["test.topic.created"].Register(services);

        // Assert
        configurator.Verify(
            c => c.Configure(It.IsAny<MiddlewareOptions<TestMessage>>()), Times.Once);
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenRegisterInvokedForDifferentConsumer_ShouldCallConfiguratorWithCorrectMessageType()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act
        map["another.topic.created"].Register(services);

        // Assert
        configurator.Verify(
            c => c.Configure(It.IsAny<MiddlewareOptions<AnotherTestMessage>>()), Times.Once);
    }

    [Fact]
    public void BuildTopicConsumerMap_DuplicateTopicName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            TopicConsumerDiscovery.BuildTopicConsumerMap(
                typeof(FirstConsumerWithSameTopic).Assembly, configurator.Object));
        Assert.Contains("Duplicate topic", exception.Message);
        Assert.Contains("duplicate.topic", exception.Message);
    }

    [Fact]
    public void BuildTopicConsumerMap_AssemblyWithNoConsumers_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(object).Assembly, configurator.Object);

        // Assert
        Assert.Empty(map);
    }

    [Fact]
    public void BuildTopicConsumerMap_AbstractHandler_ShouldNotBeDiscovered()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Assert
        Assert.DoesNotContain(map.Values, v => v.ConsumerName == nameof(SubscriberWithAbstractHandler));
    }

    [Fact]
    public void BuildTopicConsumerMap_NullAssembly_ShouldThrowArgumentNullException()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TopicConsumerDiscovery.BuildTopicConsumerMap(null!, configurator.Object));
    }

    [Fact]
    public void BuildTopicConsumerMap_NullConfigurator_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TopicConsumerDiscovery.BuildTopicConsumerMap(
                typeof(TopicConsumerDiscoveryTests).Assembly, null!));
    }

    [Fact]
    public void BuildTopicConsumerMap_WhenConfiguratorThrows_ShouldPropagateOriginalException()
    {
        // Arrange
        var configurator = new Mock<IMiddlewareConfigurator>();
        configurator
            .Setup(c => c.Configure(It.IsAny<MiddlewareOptions<TestMessage>>()))
            .Throws(new InvalidOperationException("Middleware config failed"));
        var services = new ServiceCollection();
        var map = TopicConsumerDiscovery.BuildTopicConsumerMap(
            typeof(TopicConsumerDiscoveryTests).Assembly, configurator.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            map["test.topic.created"].Register(services));
        Assert.Equal("Middleware config failed", exception.Message);
    }

    #region Test fixtures

    public class TestMessage : IMessage
    {
        public string MessageId { get; set; }
        public string MessageGroup { get; set; }
    }

    public class AnotherTestMessage : IMessage
    {
        public string MessageId { get; set; }
        public string MessageGroup { get; set; }
    }

    public class ValidConsumer : ICapSubscribe
    {
        [CapSubscribe("test.topic.created")]
        public Task HandleAsync(TestMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public class Handler : IConsumerService<TestMessage>
        {
            public Task ProcessMessageAsync(TestMessage message,
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    public class AnotherValidConsumer : ICapSubscribe
    {
        [CapSubscribe("another.topic.created")]
        public Task HandleAsync(AnotherTestMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public class Handler : IConsumerService<AnotherTestMessage>
        {
            public Task ProcessMessageAsync(AnotherTestMessage message,
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    public class ConsumerWithoutAttribute : ICapSubscribe
    {
        public Task HandleAsync(TestMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public class Handler : IConsumerService<TestMessage>
        {
            public Task ProcessMessageAsync(TestMessage message,
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    public class NotASubscriber
    {
        public class Handler : IConsumerService<TestMessage>
        {
            public Task ProcessMessageAsync(TestMessage message,
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;
        }
    }

    public class SubscriberWithAbstractHandler : ICapSubscribe
    {
        [CapSubscribe("abstract.handler.topic")]
        public Task HandleAsync(TestMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public abstract class Handler : IConsumerService<TestMessage>
        {
            public abstract Task ProcessMessageAsync(TestMessage message,
                CancellationToken cancellationToken = default);
        }
    }

    #endregion
}
