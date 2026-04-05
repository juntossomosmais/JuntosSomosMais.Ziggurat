using System.Threading.Tasks;

namespace JuntosSomosMais.Ziggurat;

public interface IConsumerService<in TMessage> where TMessage : IMessage
{
    public Task ProcessMessageAsync(TMessage message);
}
