using System.Threading.Tasks;

namespace Ziggurat;

public interface IConsumerService<in TMessage> where TMessage : IMessage
{
    public Task ProcessMessageAsync(TMessage message);
}
