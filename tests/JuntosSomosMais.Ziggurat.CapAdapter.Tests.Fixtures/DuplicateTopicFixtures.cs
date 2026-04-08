using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using JuntosSomosMais.Ziggurat;

namespace JuntosSomosMais.Ziggurat.CapAdapter.Tests.Fixtures;

public class DuplicateTopicMessage : IMessage
{
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}

public class FirstConsumerWithSameTopic : ICapSubscribe
{
    [CapSubscribe("duplicate.topic")]
    public Task HandleAsync(DuplicateTopicMessage message,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public class Handler : IConsumerService<DuplicateTopicMessage>
    {
        public Task ProcessMessageAsync(DuplicateTopicMessage message,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public class SecondConsumerWithSameTopic : ICapSubscribe
{
    [CapSubscribe("duplicate.topic")]
    public Task HandleAsync(DuplicateTopicMessage message,
        CancellationToken cancellationToken) => Task.CompletedTask;

    public class Handler : IConsumerService<DuplicateTopicMessage>
    {
        public Task ProcessMessageAsync(DuplicateTopicMessage message,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
