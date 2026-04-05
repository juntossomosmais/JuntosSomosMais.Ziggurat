using DotNetCore.CAP;
using JuntosSomosMais.Ziggurat;

namespace Sample.Cap.Mongo;

public class Consumer : ICapSubscribe
{
    private readonly IConsumerService<MyMessage> _service;

    public Consumer(IConsumerService<MyMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("mymessage.created", Group = "mongo.mymessage.created")]
    public async Task ConsumeMessage(MyMessage message, CancellationToken cancellationToken)
    {
        await _service.ProcessMessageAsync(message, cancellationToken);
    }
}
