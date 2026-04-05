using System;
using System.Threading;
using System.Threading.Tasks;
using JuntosSomosMais.Ziggurat.Internal;
using Microsoft.Extensions.Logging;

namespace JuntosSomosMais.Ziggurat.Idempotency;

internal class IdempotencyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<IdempotencyMiddleware<TMessage>> _logger;
    private readonly IStorage _storage;

    public IdempotencyMiddleware(
        IStorage storage,
        ILogger<IdempotencyMiddleware<TMessage>> logger)
    {
        _logger = logger;
        _storage = storage;
    }

    public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next, CancellationToken cancellationToken)
    {
        if (await _storage.HasProcessedAsync(message, cancellationToken))
        {
            _logger.LogMessageExists(message);
            return;
        }

        try
        {
            await next(message, cancellationToken);
        }
        catch (Exception ex) when (_storage.IsMessageExistsError(ex))
        {
            // If is unique key error it means that the message
            // was already processed and should do nothing
            _logger.LogMessageExists(message);
        }
    }
}
