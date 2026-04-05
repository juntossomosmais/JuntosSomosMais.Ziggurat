using System.Threading;
using System.Threading.Tasks;
using JuntosSomosMais.Ziggurat;
using Microsoft.Extensions.Logging;
using Sample.Cap.SqlServer.Dtos;
using Sample.Cap.SqlServer.Infrastructure;

namespace Sample.Cap.SqlServer.Domain.Services;

public class OrderCreatedConsumerService : IConsumerService<OrderCreatedMessage>
{
    private readonly ExampleDbContext _context;
    private readonly ILogger<OrderCreatedConsumerService> _logger;

    public OrderCreatedConsumerService(
        ExampleDbContext context,
        ILogger<OrderCreatedConsumerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(OrderCreatedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Got {message}", message);
        // Do something
        await _context.SaveChangesAsync(cancellationToken);
    }
}
