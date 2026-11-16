using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NeoWatch.Core.Abstractions;

namespace NeoWatch.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers everything that talks to the outside world: HTTP and the database.</summary>
    public static IServiceCollection AddNeoWatchInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NeoWsOptions>(configuration.GetSection(NeoWsOptions.SectionName));

        services.AddDbContext<NeoWatchDbContext>(db =>
            db.UseSqlite(configuration.GetConnectionString("NeoWatch") ?? "Data Source=neowatch.db"));

        services.AddScoped<IApproachRepository, ApproachRepository>();

        services
            .AddHttpClient<INeoWsClient, NeoWsClient>((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<NeoWsOptions>>().Value;
                http.BaseAddress = new Uri(options.BaseUrl);
            })
            // Retries with exponential backoff on 5xx, 408, 429 and network errors,
            // plus a circuit breaker and timeouts. Built on Polly.
            .AddStandardResilienceHandler();

        return services;
    }
}
