using FastFoodMcp.Infra;
using FastFoodMcp.Models;
using FastFoodMcpBase.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastFoodMcp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonStores(this IServiceCollection services)
    {
        // Get data file paths
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        var errorsPath = Path.Combine(dataPath, "errors.json");
        var systemPath = Path.Combine(dataPath, "system.json");
        var flagsPath = Path.Combine(dataPath, "flags.json");
        
        // Register JSON stores as singletons with hot-reload capability
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonStore<Dictionary<string, ErrorEntry>>>>();
            return new JsonStore<Dictionary<string, ErrorEntry>>(errorsPath, logger);
        });

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonStore<SystemData>>>();
            return new JsonStore<SystemData>(systemPath, logger);
        });

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<JsonStore<FlagsData>>>();
            return new JsonStore<FlagsData>(flagsPath, logger);
        });

        return services;
    }
}