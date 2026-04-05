using System;
using Xunit;

namespace JuntosSomosMais.Ziggurat.MongoDB.Tests;

[Collection("TestFixture Collection")]
public class ZigguratMongoDbOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MongoDatabaseName_IsNullOrEmpty_ThrowsException(string input)
    {
        // Arrange
        var original = ZigguratMongoDbOptions.MongoDatabaseName;
        ZigguratMongoDbOptions.MongoDatabaseName = input;

        try
        {
            // Act & Assert
            var exception =
                Assert.Throws<InvalidOperationException>(() => _ = ZigguratMongoDbOptions.MongoDatabaseName);
            const string expectedMessage =
                "MongoDB database name must be set. Be sure you are calling `UseMongoDbIdempotency`.";
            Assert.Equal(expectedMessage, exception.Message);
        }
        finally
        {
            ZigguratMongoDbOptions.MongoDatabaseName = original;
        }
    }
}
