using System.Linq;
using JuntosSomosMais.Ziggurat.Idempotency;
using JuntosSomosMais.Ziggurat.SqlServer.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JuntosSomosMais.Ziggurat.SqlServer.Tests;

public class CleanerOptionsExtensionsTests
{
    [Fact]
    public void UseEntityFrameworkStorage_ShouldRegister_EntityFrameworkStorage()
    {
        // Arrange
        var options = new CleanerOptions();

        // Act
        options.UseEntityFrameworkStorage<TestDbContext>();

        // Assert
        var services = new ServiceCollection();
        options.RegisterStorage(services);
        var storage = services.FirstOrDefault(x => x.ServiceType == typeof(IStorage));
        Assert.NotNull(storage);
        Assert.Equal(typeof(EntityFrameworkStorage<TestDbContext>), storage.ImplementationType);
    }
}
