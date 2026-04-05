using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JuntosSomosMais.Ziggurat.Tests;

public class PipelineTests
{
    [Fact]
    public async Task RunPipeline_MultipleMiddlewares_RunInOrder()
    {
        // Arrange
        var order = new List<string>(3);
        var services = new ServiceCollection();
        services.AddSingleton(order);
        services.AddScoped<TestConsumerService>();
        services.AddScoped<IConsumerMiddleware<TestMessage>, TestMiddleware1>();
        services.AddScoped<IConsumerMiddleware<TestMessage>, TestMiddleware2>();
        services.AddScoped<IConsumerService<TestMessage>>(t => new PipelineHandler<TestMessage>(
            t,
            t.GetRequiredService<TestConsumerService>()
        ));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var pipeline = serviceProvider.GetRequiredService<IConsumerService<TestMessage>>();
        await pipeline.ProcessMessageAsync(new TestMessage(), TestContext.Current.CancellationToken);

        // Assert - Verify call order
        Assert.Equal(new()
        {
            "TestMiddleware1",
            "TestMiddleware2",
            "TestConsumerService"
        }, order);
    }

    [Fact]
    public async Task RunPipeline_WithCancellationToken_PropagatesTokenToService()
    {
        // Arrange
        CancellationToken receivedToken = default;
        var services = new ServiceCollection();
        services.AddScoped<IConsumerService<TestMessage>>(sp =>
        {
            var inner = new TokenCapturingConsumerService(ct => receivedToken = ct);
            return new PipelineHandler<TestMessage>(sp, inner);
        });
        var serviceProvider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource();

        // Act
        var pipeline = serviceProvider.GetRequiredService<IConsumerService<TestMessage>>();
        await pipeline.ProcessMessageAsync(new TestMessage(), cts.Token);

        // Assert
        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task RunPipeline_WithCancellationToken_PropagatesTokenThroughMiddleware()
    {
        // Arrange
        CancellationToken tokenInMiddleware = default;
        CancellationToken tokenInService = default;
        var services = new ServiceCollection();
        services.AddScoped<IConsumerMiddleware<TestMessage>>(
            _ => new TokenCapturingMiddleware(ct => tokenInMiddleware = ct));
        services.AddScoped<IConsumerService<TestMessage>>(sp =>
        {
            var inner = new TokenCapturingConsumerService(ct => tokenInService = ct);
            return new PipelineHandler<TestMessage>(sp, inner);
        });
        var serviceProvider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource();

        // Act
        var pipeline = serviceProvider.GetRequiredService<IConsumerService<TestMessage>>();
        await pipeline.ProcessMessageAsync(new TestMessage(), cts.Token);

        // Assert
        Assert.Equal(cts.Token, tokenInMiddleware);
        Assert.Equal(cts.Token, tokenInService);
    }

    public class TestMessage : IMessage
    {
        public string MessageId { get; set; }
        public string MessageGroup { get; set; }
    }

    public class TestConsumerService : IConsumerService<TestMessage>
    {
        private readonly List<string> _order;

        public TestConsumerService(List<string> order)
        {
            _order = order;
        }

        public Task ProcessMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            _order.Add("TestConsumerService");
            return Task.CompletedTask;
        }
    }

    public class TestMiddleware1 : IConsumerMiddleware<TestMessage>
    {
        private readonly List<string> _order;

        public TestMiddleware1(List<string> order)
        {
            _order = order;
        }

        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            _order.Add("TestMiddleware1");
            await next(message, cancellationToken);
        }
    }

    public class TestMiddleware2 : IConsumerMiddleware<TestMessage>
    {
        private readonly List<string> _order;

        public TestMiddleware2(List<string> order)
        {
            _order = order;
        }

        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            _order.Add("TestMiddleware2");
            await next(message, cancellationToken);
        }
    }

    private class TokenCapturingConsumerService : IConsumerService<TestMessage>
    {
        private readonly Action<CancellationToken> _capture;

        public TokenCapturingConsumerService(Action<CancellationToken> capture)
        {
            _capture = capture;
        }

        public Task ProcessMessageAsync(TestMessage message, CancellationToken cancellationToken = default)
        {
            _capture(cancellationToken);
            return Task.CompletedTask;
        }
    }

    private class TokenCapturingMiddleware : IConsumerMiddleware<TestMessage>
    {
        private readonly Action<CancellationToken> _capture;

        public TokenCapturingMiddleware(Action<CancellationToken> capture)
        {
            _capture = capture;
        }

        public async Task OnExecutingAsync(TestMessage message, ConsumerServiceDelegate<TestMessage> next, CancellationToken cancellationToken)
        {
            _capture(cancellationToken);
            await next(message, cancellationToken);
        }
    }
}
