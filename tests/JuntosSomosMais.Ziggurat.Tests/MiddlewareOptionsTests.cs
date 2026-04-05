using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JuntosSomosMais.Ziggurat.Tests;

public class MiddlewareOptionsTests
{
    [Fact]
    public void Use_ShouldAddExtensionToList()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();

        // Act
        options.Use<TestMiddleware>();

        // Assert
        Assert.Single(options.Extensions);
    }

    [Fact]
    public void Use_MultipleTimes_ShouldAddMultipleExtensions()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();

        // Act
        options.Use<TestMiddleware>();
        options.Use<AnotherTestMiddleware>();

        // Assert
        Assert.Equal(2, options.Extensions.Count);
    }

    [Fact]
    public void Use_ExtensionShouldRegisterMiddlewareInServiceCollection()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();
        var services = new ServiceCollection();

        // Act
        options.Use<TestMiddleware>();
        foreach (var extension in options.Extensions)
            extension(services);

        var provider = services.BuildServiceProvider();
        var middlewares = provider.GetServices<IConsumerMiddleware<TestMessage>>();

        // Assert
        Assert.Single(middlewares);
        Assert.IsType<TestMiddleware>(middlewares.First());
    }

    [Fact]
    public void Extensions_ShouldBeEmptyByDefault()
    {
        // Arrange & Act
        var options = new MiddlewareOptions<TestMessage>();

        // Assert
        Assert.Empty(options.Extensions);
    }

    public class TestMessage : IMessage
    {
        public string MessageId { get; set; }
        public string MessageGroup { get; set; }
    }

    public class TestMiddleware : IConsumerMiddleware<TestMessage>
    {
        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            await next(message, cancellationToken);
        }
    }

    public class AnotherTestMiddleware : IConsumerMiddleware<TestMessage>
    {
        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            await next(message, cancellationToken);
        }
    }
}
