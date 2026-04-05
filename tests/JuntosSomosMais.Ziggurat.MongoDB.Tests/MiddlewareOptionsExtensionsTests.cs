using System.Linq;
using JuntosSomosMais.Ziggurat.Idempotency;
using JuntosSomosMais.Ziggurat.MongoDB.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JuntosSomosMais.Ziggurat.MongoDB.Tests;

public class MiddlewareOptionsExtensionsTests
{
    [Fact]
    public void UseMongoDbIdempotency_ShouldRegister_MongoDbStorage()
    {
        // Arrange
        var options = new MiddlewareOptions<TestMessage>();
        const string mongoDatabaseName = "test";

        // Act
        options.UseMongoDbIdempotency(mongoDatabaseName);

        // Assert
        var services = new ServiceCollection();
        foreach (var extention in options.Extensions)
        {
            extention(services);
        }

        var storage = services
            .FirstOrDefault(x => x.ServiceType == typeof(IStorage) &&
                            x.ImplementationType == typeof(MongoDbStorage));
        Assert.NotNull(storage);
        var idempotency = services
            .FirstOrDefault(x => x.ServiceType == typeof(IConsumerMiddleware<TestMessage>) &&
                            x.ImplementationType == typeof(IdempotencyMiddleware<TestMessage>));
        Assert.NotNull(idempotency);
    }
}
