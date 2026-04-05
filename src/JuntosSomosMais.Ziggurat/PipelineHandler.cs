using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace JuntosSomosMais.Ziggurat;

internal class PipelineHandler<TMessage> : IConsumerService<TMessage>
    where TMessage : IMessage
{
    private readonly IConsumerService<TMessage> _service;
    private readonly IServiceProvider _serviceProvider;

    public PipelineHandler(IServiceProvider serviceProvider, IConsumerService<TMessage> service)
    {
        _serviceProvider = serviceProvider;
        _service = service;
    }

    public async Task ProcessMessageAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        var middlewares = _serviceProvider.GetServices<IConsumerMiddleware<TMessage>>();

        var stack = new Stack<ConsumerServiceDelegate<TMessage>>();
        stack.Push((consumerMessage, ct) => _service.ProcessMessageAsync(consumerMessage, ct));
        foreach (var middleware in middlewares.Reverse())
            stack.Push((consumerMessage, ct) => middleware.OnExecutingAsync(consumerMessage, stack.Pop(), ct));

        await stack.Pop()(message, cancellationToken);
    }
}

public delegate Task ConsumerServiceDelegate<in TMessage>(TMessage message, CancellationToken cancellationToken)
    where TMessage : IMessage;

public interface IConsumerMiddleware<TMessage> where TMessage : IMessage
{
    public Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next, CancellationToken cancellationToken);
}
