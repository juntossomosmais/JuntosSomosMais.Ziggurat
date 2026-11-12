using System;
using System.Threading;
using System.Threading.Tasks;
using JuntosSomosMais.Ziggurat.Idempotency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JuntosSomosMais.Ziggurat.Tests;

public class CleanerTests
{
    // Since Microsoft.Extensions.Hosting.Abstractions 10, BackgroundService.StartAsync no longer runs the
    // synchronous part of ExecuteAsync before returning, so tests must wait for the cleaner to reach the expected point.
    private static readonly TimeSpan LogTimeout = TimeSpan.FromSeconds(10);

    private static Task WhenLogged(Mock<ILogger<Cleaner>> logger, LogLevel level, string message)
    {
        var logged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // CA1873 does not apply: this is a Moq setup expression tree, never executed as a logging call.
#pragma warning disable CA1873
        logger
            .Setup(x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == message),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Callback(() => logged.TrySetResult());
#pragma warning restore CA1873
        return logged.Task;
    }

    [Fact]
    public async Task StartAsync_ShouldCallStorageDeleteAndLog()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        var removedLogged = WhenLogged(mockLogger, LogLevel.Information, "Ziggurat cleaner background service removed 10 messages.");
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        mockStorage
            .Setup(x => x.DeleteMessagesHistoryOlderThanAsync(optionsExpireAfterInDays, batchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Verifiable();
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);
        await removedLogged.WaitAsync(LogTimeout, TestContext.Current.CancellationToken);

        // Assert
        mockStorage.Verify();
        mockLogger.VerifyLog(x => x.LogInformation($"Ziggurat cleaner background service removed 10 messages."));
        await cancellationTokenSource.CancelAsync();
    }

    [Fact]
    public async Task StartAsync_WhenCancellationTokenIsCalled_ShouldStopCleaner()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        var stoppingLogged = WhenLogged(mockLogger, LogLevel.Information, "Ziggurat cleaner background service is stopping.");
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        // We need to call DeleteMessagesHistoryOlderThanAsync twice to stop the cleaner
        var deletedCount = 0;
        mockStorage
            .Setup(x => x.DeleteMessagesHistoryOlderThanAsync(optionsExpireAfterInDays, batchSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10)
            .Callback(() =>
            {
                deletedCount++;
                if (deletedCount == 2)
                {
                    cancellationTokenSource.Cancel();
                }
            });
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);

        await stoppingLogged.WaitAsync(LogTimeout, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(cancellationTokenSource.IsCancellationRequested);
        mockStorage.Verify();
        mockLogger.VerifyLog(x => x.LogInformation("Ziggurat cleaner background service is stopping."));
    }

    [Fact]
    public void StartAsync_WhenStorageInitializeThrowError_ShouldStopBackgroundService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = 1;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IHostedService>());

        // Assert
        Assert.Equal("Cannot create Ziggurat cleaner background service because IStorage is not registered.", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenDeleteThrowsException_ShouldLogErrorAndContinue()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        var deleteErrorLogged = WhenLogged(mockLogger, LogLevel.Error, "Ziggurat cleaner found an error occurred while deleting old messages.");
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var callCount = 0;
        mockStorage
            .Setup(x => x.DeleteMessagesHistoryOlderThanAsync(optionsExpireAfterInDays, batchSize, It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("DB connection failed");
                cancellationTokenSource.Cancel();
                return Task.FromResult(5);
            });
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        var removedAfterErrorLogged = WhenLogged(mockLogger, LogLevel.Information, "Ziggurat cleaner background service removed 5 messages.");
        await hostedService.StartAsync(cancellationTokenSource.Token);
        await deleteErrorLogged.WaitAsync(LogTimeout, TestContext.Current.CancellationToken);
        await removedAfterErrorLogged.WaitAsync(LogTimeout, TestContext.Current.CancellationToken);

        // Assert
        mockLogger.VerifyLog(x => x.LogError("Ziggurat cleaner found an error occurred while deleting old messages."));
    }

    [Fact]
    public void AddZigguratCleaner_ParameterlessOverload_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockStorage = new Mock<IStorage>();
        services.AddScoped(_ => mockStorage.Object);
        var mockLogger = new Mock<ILogger<Cleaner>>();
        services.AddSingleton(_ => mockLogger.Object);

        // Act
        services.AddZigguratCleaner();
        var provider = services.BuildServiceProvider();

        // Assert
        var hostedService = provider.GetRequiredService<IHostedService>();
        Assert.NotNull(hostedService);
    }

    [Fact]
    public async Task Ctor_WhenStorageIsNotRegistered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const int optionsExpireAfterInDays = 1;
        const int batchSize = 100;
        var cancellationTokenSource = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.AddZigguratCleaner(options =>
        {
            options.CleaningInterval = TimeSpan.FromSeconds(1);
            options.ExpireAfterInDays = optionsExpireAfterInDays;
            options.BatchSize = batchSize;
        });
        var mockLogger = new Mock<ILogger<Cleaner>>();
        var initializationErrorLogged = WhenLogged(mockLogger, LogLevel.Error, "Could not initialize Ziggurat Cleaner properly. Cleaner is shutting up.");
        services.AddSingleton(_ => mockLogger.Object);
        var mockStorage = new Mock<IStorage>();
        mockStorage
            .Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Throws<InvalidOperationException>();

        // Act
        services.AddScoped(_ => mockStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var hostedService = serviceProvider.GetRequiredService<IHostedService>();
        await hostedService.StartAsync(cancellationTokenSource.Token);
        await initializationErrorLogged.WaitAsync(LogTimeout, TestContext.Current.CancellationToken);

        // Assert
        mockLogger.VerifyLog(x => x.LogError("Could not initialize Ziggurat Cleaner properly. Cleaner is shutting up."));
    }
}
