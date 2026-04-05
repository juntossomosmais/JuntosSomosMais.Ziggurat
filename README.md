# ![Ziggurat icon](./docs/icon.png) Ziggurat

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=alert_status)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=rafaelpadovezi_Ziggurat&metric=coverage)](https://sonarcloud.io/dashboard?id=rafaelpadovezi_Ziggurat)

A .NET library to create message consumers.

Ziggurat implements functionalities to help solve common problems when dealing with messages:
- [Idempotency](https://microservices.io/patterns/communication-style/idempotent-consumer.html)
- Middleware: allows to create middlewares to consumers to handle logging, validation and whatever is needed. 

## How it works

The library uses the [decorator pattern](https://refactoring.guru/design-patterns/decorator/csharp/example) to execute a middleware pipeline when calling the consumer services. This way is possible to extend the service code adding new functionality.

The Idempotency middleware wraps the service enforcing that the message in only being processed once by tracking the message processing on the database.

Also, it's possible to add custom middlewares to the pipeline.

## Support

Ziggurat has support to:
- Storage:
  - MS SQL Server
  - MongoDB
- Messaging Library
  - [CAP](https://cap.dotnetcore.xyz/)

## Install

|                                        |                                                                                                              |
|----------------------------------------|--------------------------------------------------------------------------------------------------------------|
| JuntosSomosMais.Ziggurat              | [![Nuget](https://img.shields.io/nuget/v/JuntosSomosMais.Ziggurat)](https://www.nuget.org/packages/JuntosSomosMais.Ziggurat)                 |
| JuntosSomosMais.Ziggurat.CapAdapter   | [![Nuget](https://img.shields.io/nuget/v/JuntosSomosMais.Ziggurat.CapAdapter)](https://www.nuget.org/packages/JuntosSomosMais.Ziggurat.CapAdapter) |
| JuntosSomosMais.Ziggurat.SqlServer    | [![Nuget](https://img.shields.io/nuget/v/JuntosSomosMais.Ziggurat.SqlServer)](https://www.nuget.org/packages/JuntosSomosMais.Ziggurat.SqlServer) |
| JuntosSomosMais.Ziggurat.MongoDB      | [![Nuget](https://img.shields.io/nuget/v/JuntosSomosMais.Ziggurat.MongoDB)](https://www.nuget.org/packages/JuntosSomosMais.Ziggurat.MongoDB) |

## Usage

Ziggurat works with middlewares. Registering middlewares adds functionality to the message consumer. Important to note that multiple middlewares can be registered to the same consumer. They are executed following the order of the registration.

### SQL Server with Entity Framework

Ziggurat integrates with the application [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) to track the processed messages and ensures that each message is processed only once. Also, the EF Core migrations are used to create the message tracking table with the correct constraints. If you are not using migration in your project the table must be created manually.

To use Ziggurat is necessary to create a message and a consumer service type:

```c#
public class MyMessage : IMessage
{
    public string Content { get; set; }
    public string MessageId { get; set; }
    public string MessageGroup { get; set; }
}

public class MyMessageConsumerService : IConsumerService<MyMessage>
{
    private readonly MyDbContext _context;

    public MyMessageConsumerService(MyDbContext context)
    {
        _context = context;
    }

    public async Task ProcessMessageAsync(MyMessage message, CancellationToken cancellationToken = default)
    {
        // Change the application bussiness objects tracked by EF Core
        _context.SomeEntity.Add(x);
        await _context.SaveChangesAsync(cancellationToken);
    }
} 
```

JuntosSomosMais.Ziggurat.SqlServer ensures that the processed messages are tracked by the EF Core `DbContext`. Calling `SaveChangesAsync` will save the changes made to the business objects and the processed message to the DB.

The message type must implements the interface `IMessage`.

It's also required that the consumers are setup on the dependency injection configuration. Besides, it's necessary to add the CAP filter that enriches the message with the required information.


```c#
services
    .AddConsumerService<MyMessage, MyConsumerService>(
        options =>
        {
            options.UseEntityFrameworkIdempotency<MyMessage, MyDbContext>();
        });
services.
    .AddCap(x => ...)
    .AddSubscribeFilter<BootstrapFilter>();
```

And finally, the the message tracking DbSet must be added to the DbContext:

```c#
public class MyDbContext : DbContext
{
    public DbSet<MessageTracking> Messages { get; set; }
    ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.MapMessageTracker();
    }
}
```

### MongoDB

Using Ziggurat with MongoDB has some differences compared to SQL Server. The dependency injection registration must call the method `UseMongoDbIdempotency`:

```c#
builder.Services.AddConsumerService<MyMessage, ConsumerService>(
    options => options.UseMongoDbIdempotency("databaseName"));
```

To keep the consumer operation atomic, is necessary to use the method `StartIdempotentTransaction``:

```c#

public class MyMessageConsumerService : IConsumerService<MyMessage>
{
    private readonly IMongoClient _client;

    public MyMessageConsumerService(IMongoClient client)
    {
        _client = client;
    }

    public async Task ProcessMessageAsync(MyMessage message, CancellationToken cancellationToken = default)
    {
        using var session = _client.StartIdempotentTransaction(message);
        // save business object
        var collection = _client.GetDatabase("databaseName").GetCollection<SomeEntity>("someEntity");
        await collection.InsertOneAsync(session, x, cancellationToken: cancellationToken);
        // must commit transaction
        await session.CommitTransactionAsync(cancellationToken);
    }
}
```
### Logging middleware

Since version 8.0.0, Ziggurat has a built-in middleware to log the message processing. It's possible to use it by calling the method `UseLoggingMiddleware`:

```c#
services
    .AddConsumerService<MyMessage, MyConsumerService>(
        options =>
        {
            options.UseLoggingMiddleware<MyMessage>();
        });
```

### Custom middleware

It's possible to create custom middleware for the consumers.

```c#
public class MyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
    where TMessage : IMessage
{
   public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next, CancellationToken cancellationToken)
    {
        // Do something before
        await next(message, cancellationToken);
        // Do something after
    }
}
```

Also, it's required to register the middleware on the dependency injection configuration.

```c#
.AddConsumerService<MyMessage, MyMessageConsumerService>(
    options =>
    {
        options.Use<LoggingMiddleware<MyMessage>>();
    });
```

Important to note that multiple middlewares can be registered to the same consumer. They are executed following the order of the registration.

You can look at the samples folder to see more examples of usage.

### CancellationToken support

The middleware pipeline propagates a `CancellationToken` through every layer: from the consumer entry point, through each middleware, and into the final consumer service. This enables cooperative cancellation during application shutdown or client disconnection.

All interfaces accept a `CancellationToken`:

- `IConsumerService<TMessage>.ProcessMessageAsync(TMessage message, CancellationToken cancellationToken = default)`
- `IConsumerMiddleware<TMessage>.OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next, CancellationToken cancellationToken)`
- `ConsumerServiceDelegate<TMessage>(TMessage message, CancellationToken cancellationToken)`

Pass the token from your CAP consumer:

```c#
public class MyConsumer : ICapSubscribe
{
    private readonly IConsumerService<MyMessage> _service;

    public MyConsumer(IConsumerService<MyMessage> service)
    {
        _service = service;
    }

    [CapSubscribe("my.topic", Group = "my.group")]
    public async Task ConsumeMessage(MyMessage message, CancellationToken cancellationToken)
    {
        await _service.ProcessMessageAsync(message, cancellationToken);
    }
}
```

Built-in middlewares (`LoggingMiddleware`, `IdempotencyMiddleware`) forward the token automatically. Custom middlewares must pass `cancellationToken` when calling `next` and to any async operations they perform.

### Clean old message tracking records

The library provides a method to clean old message tracking records. A background service can be added using the extension method `AddZigguratCleaner`:

```c#
services.AddZigguratCleaner(options => {
    options.CleaningInterval = TimeSpan.FromMinutes(15);
    options.ExpireAfterInDays = 7;
    options.BatchSize = 100_000; // Only works with SQL Server
});
```

## Run tests

```shell
docker compose run --rm --remove-orphans integration-tests
```