using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace JuntosSomosMais.Ziggurat.Logging;

public class LoggingMiddleware<TMessage> : IConsumerMiddleware<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingMiddleware<TMessage>> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware<TMessage>> logger)
    {
        _logger = logger;
    }

    public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stopWatch = new Stopwatch();
        stopWatch.Start();
        try
        {
            await next(message, cancellationToken);
            stopWatch.Stop();
            _logger.LogInformation("Executed {MessageGroup}:{MessageId} in {Elapsed:000} ms.",
                message.MessageGroup,
                message.MessageId,
                stopWatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopWatch.Stop();
            _logger.LogError(ex, "Executed {MessageGroup}:{MessageId} with error in {Elapsed:000} ms.",
                message.MessageGroup,
                message.MessageId,
                stopWatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
