using JuntosSomosMais.Ziggurat.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JuntosSomosMais.Ziggurat.SqlServer;

public static class CleanerOptionsExtensions
{
    public static CleanerOptions UseEntityFrameworkStorage<TContext>(this CleanerOptions options) where TContext : DbContext
    {
        options.RegisterStorage = services =>
        {
            services.TryAddScoped<IStorage, EntityFrameworkStorage<TContext>>();
        };
        return options;
    }
}
