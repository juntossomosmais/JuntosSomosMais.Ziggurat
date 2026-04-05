using System;
using MongoDB.Driver;
using Xunit;

namespace JuntosSomosMais.Ziggurat.MongoDB.Tests.Support;

[Collection("TestFixture Collection")]
public class TestFixture
{
    public TestFixture()
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDb");
        if (string.IsNullOrWhiteSpace(mongoConnectionString))
            mongoConnectionString = "mongodb://localhost:27017?directConnection=true";
        ZigguratMongoDbOptions.MongoDatabaseName = $"test{Guid.NewGuid()}";
        MongoClient = new MongoClient(mongoConnectionString);
        MongoDatabase = MongoClient.GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName);
    }

    protected IMongoClient MongoClient { get; }
    protected IMongoDatabase MongoDatabase { get; }
}
