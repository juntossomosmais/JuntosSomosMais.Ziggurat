using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;

namespace JuntosSomosMais.Ziggurat.CapAdapter;

/// <summary>
/// CAP filter that sets the message with the message Id and message Group, which will be
/// used by Ziggurat pipeline.
/// </summary>
public class BootstrapFilter : SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        var message = context.Arguments
            .FirstOrDefault(x => x is IMessage) ?? throw new InvalidOperationException("Message must be of type IMessage");
        ((IMessage)message).MessageId = context.DeliverMessage.GetId();
        ((IMessage)message).MessageGroup = context.DeliverMessage.GetGroup();
        return Task.CompletedTask;
    }
}
