namespace JuntosSomosMais.Ziggurat.CapAdapter;

/// <summary>
/// Defines the middleware configuration for consumer services discovered by <see cref="TopicConsumerDiscovery"/>.
/// Implement this interface in each application to configure the middleware pipeline
/// (e.g., idempotency, validation) that applies to all discovered consumers.
/// </summary>
public interface IMiddlewareConfigurator
{
    /// <summary>
    /// Configures the middleware pipeline for a consumer service.
    /// </summary>
    /// <typeparam name="TMessage">The message type of the consumer</typeparam>
    /// <param name="options">The middleware options to configure</param>
    public void Configure<TMessage>(MiddlewareOptions<TMessage> options) where TMessage : IMessage;
}
