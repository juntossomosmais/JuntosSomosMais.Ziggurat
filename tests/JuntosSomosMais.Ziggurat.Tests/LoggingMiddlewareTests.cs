using System;
using System.Threading;
using System.Threading.Tasks;
using JuntosSomosMais.Ziggurat.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JuntosSomosMais.Ziggurat.Tests;

public class LoggingMiddlewareTests
{
    private readonly Mock<ILogger<LoggingMiddleware<TestMessage>>> _mockLogger;
    private readonly LoggingMiddleware<TestMessage> _middleware;

    public LoggingMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<LoggingMiddleware<TestMessage>>>();
        _middleware = new LoggingMiddleware<TestMessage>(_mockLogger.Object);
    }

    [Fact]
    public async Task OnExecutingAsync_ExecutesSuccessfully_LogsInformation()
    {
        var testMessage = new TestMessage
        {
            MessageGroup = "group1",
            MessageId = "message1"
        };
        var mockDelegate = new Mock<ConsumerServiceDelegate<TestMessage>>();
        mockDelegate.Setup(d => d(testMessage, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _middleware.OnExecutingAsync(testMessage, mockDelegate.Object, CancellationToken.None);

        _mockLogger.VerifyLog(
            x => x.LogInformation("Executed group1:message1 in * ms."), Times.Once);
    }

    [Fact]
    public async Task OnExecutingAsync_ThrowsException_LogsError()
    {
        var testMessage = new TestMessage
        {
            MessageGroup = "group1",
            MessageId = "message1"
        };
        var mockDelegate = new Mock<ConsumerServiceDelegate<TestMessage>>();
        mockDelegate.Setup(d => d(testMessage, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await Assert.ThrowsAsync<Exception>(() => _middleware.OnExecutingAsync(testMessage, mockDelegate.Object, CancellationToken.None));

        _mockLogger.VerifyLog(
            x => x.LogError("Executed group1:message1 with error in * ms."), Times.Once);
    }

    [Fact]
    public async Task OnExecutingAsync_CancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        var testMessage = new TestMessage
        {
            MessageGroup = "group1",
            MessageId = "message1"
        };
        var mockDelegate = new Mock<ConsumerServiceDelegate<TestMessage>>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _middleware.OnExecutingAsync(testMessage, mockDelegate.Object, cts.Token));
        mockDelegate.Verify(d => d(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class TestMessage : IMessage
{
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}
