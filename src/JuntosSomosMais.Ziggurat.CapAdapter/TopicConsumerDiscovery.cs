using System.Reflection;
using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;

namespace JuntosSomosMais.Ziggurat.CapAdapter;

/// <summary>
/// Discovers CAP consumer types following the convention of nested <see cref="IConsumerService{TMessage}"/> handlers
/// inside <see cref="ICapSubscribe"/> subscriber classes, and builds a topic-to-consumer registration map.
/// <para>
/// Convention: each subscriber class implements <see cref="ICapSubscribe"/> and contains a nested
/// <c>Handler</c> class implementing <see cref="IConsumerService{TMessage}"/>. The topic name is
/// extracted from the first <see cref="CapSubscribeAttribute"/> found on the subscriber's public methods.
/// Only one <see cref="CapSubscribeAttribute"/> per subscriber class is supported; additional attributes
/// on other methods are ignored.
/// </para>
/// </summary>
public static class TopicConsumerDiscovery
{
    /// <summary>
    /// Scans the specified assembly for <see cref="ICapSubscribe"/> types with nested
    /// <see cref="IConsumerService{TMessage}"/> handlers and builds a dictionary mapping topic names
    /// to consumer registration actions.
    /// </summary>
    /// <param name="assembly">The assembly to scan for consumer types</param>
    /// <param name="configurator">The middleware configurator to apply to each consumer</param>
    /// <returns>
    /// A dictionary where keys are topic names from <see cref="CapSubscribeAttribute"/> and values are
    /// tuples containing the consumer class name and a registration action for the service collection.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="assembly"/> or <paramref name="configurator"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when two subscriber classes share the same topic name.</exception>
    public static Dictionary<string, (string ConsumerName, Action<IServiceCollection> Register)>
        BuildTopicConsumerMap(Assembly assembly, IMiddlewareConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(configurator);

        var map = new Dictionary<string, (string ConsumerName, Action<IServiceCollection> Register)>();
        var registerMethod = typeof(TopicConsumerDiscovery)
            .GetMethod(nameof(RegisterConsumer), BindingFlags.NonPublic | BindingFlags.Static)!;

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: true })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerService<>)));

        foreach (var handlerType in handlerTypes)
        {
            var subscriberType = handlerType.DeclaringType;
            if (subscriberType is null || !typeof(ICapSubscribe).IsAssignableFrom(subscriberType))
                continue;

            var messageType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerService<>))
                .GetGenericArguments()[0];

            var topicName = subscriberType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(m => m.GetCustomAttributes<CapSubscribeAttribute>())
                .Select(a => a.Name)
                .FirstOrDefault();

            if (topicName is null)
                continue;

            if (map.TryGetValue(topicName, out var existing))
                throw new InvalidOperationException(
                    $"Duplicate topic '{topicName}' found on subscriber '{subscriberType.FullName}'. " +
                    $"A consumer for this topic was already registered by '{existing.ConsumerName}'.");

            var genericMethod = registerMethod.MakeGenericMethod(subscriberType, messageType, handlerType);
            map[topicName] = (
                subscriberType.Name,
                services =>
                {
                    try
                    {
                        genericMethod.Invoke(null, new object[] { services, configurator });
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is not null)
                    {
                        throw ex.InnerException;
                    }
                }
            );
        }

        return map;
    }

    private static void RegisterConsumer<TSubscriber, TMessage, THandler>(
        IServiceCollection services,
        IMiddlewareConfigurator configurator)
        where TSubscriber : class, ICapSubscribe
        where TMessage : IMessage
        where THandler : class, IConsumerService<TMessage>
    {
        services.AddScoped<TSubscriber>();
        services.AddConsumerService<TMessage, THandler>(options => configurator.Configure(options));
    }
}
