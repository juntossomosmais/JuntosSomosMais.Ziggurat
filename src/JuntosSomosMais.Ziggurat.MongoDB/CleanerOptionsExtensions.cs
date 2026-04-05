using JuntosSomosMais.Ziggurat.Idempotency;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JuntosSomosMais.Ziggurat.MongoDB;

public static class CleanerOptionsExtensions
{
    public static CleanerOptions UseMongoDbStorage(this CleanerOptions options)
    {
        options.RegisterStorage = services =>
        {
            services.TryAddScoped<IStorage, MongoDbStorage>();
        };
        return options;
    }
}
