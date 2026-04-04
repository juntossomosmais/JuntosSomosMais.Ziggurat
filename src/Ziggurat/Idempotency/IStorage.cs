namespace Ziggurat.Idempotency;

public interface IStorage
{
    public bool IsMessageExistsError(Exception ex);

    public Task<bool> HasProcessedAsync(IMessage message);

    public Task<int> DeleteMessagesHistoryOlderThanAsync(int days, int batchSize, CancellationToken cancellationToken);

    public Task InitializeAsync(CancellationToken stoppingToken);
}
