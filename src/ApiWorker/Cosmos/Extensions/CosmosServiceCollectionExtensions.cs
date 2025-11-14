using ApiWorker.Cosmos.Settings;
using Microsoft.Azure.Cosmos;

namespace ApiWorker.Cosmos.Extensions;

/// <summary>
/// Extension methods for registering Azure Cosmos DB services in DI container.
/// Cosmos DB is used for storing high-volume, schema-flexible data like transcripts.
/// </summary>
public static class CosmosServiceCollectionExtensions
{
    /// <summary>
    /// Registers Cosmos DB client and containers for the application.
    /// Creates database and containers if they don't exist (controlled by settings).
    /// </summary>
    public static IServiceCollection AddCosmosInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Bind configuration section to strongly-typed settings
        var section = config.GetSection("Cosmos");
        services.Configure<CosmosSettings>(section);
        var settings = section.Get<CosmosSettings>() ?? new CosmosSettings();

        // Create Cosmos client with connection settings
        // CosmosClient is thread-safe and should be singleton for connection pooling
        var client = new CosmosClient(settings.Endpoint, settings.Key,
            new CosmosClientOptions 
            { 
                ApplicationName = "BiasharaOS-API",
                // Recommended: Use direct mode for better performance
                ConnectionMode = ConnectionMode.Direct,
                // Limit retries for faster failure detection
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(10)
            });

        services.AddSingleton(client);

        // Auto-create database and containers for development convenience
        // In production, these should be pre-created with proper throughput settings
        if (settings.CreateIfNotExists)
        {
            // Create database if it doesn't exist
            var db = client.CreateDatabaseIfNotExistsAsync(settings.Database).GetAwaiter().GetResult().Database;
            
            // Create transcripts container with /businessId partition key
            // Partition key choice is critical: all queries should include businessId for efficiency
            db.CreateContainerIfNotExistsAsync(
                new ContainerProperties(settings.Containers.Transcripts, "/businessId")
                {
                    // Optional: Set default TTL for auto-cleanup of old transcripts
                    // DefaultTimeToLive = (int)TimeSpan.FromDays(90).TotalSeconds
                }
            ).GetAwaiter().GetResult();
        }

        // Register the transcripts container as a singleton
        // This allows direct injection of Container instead of CosmosClient
        services.AddSingleton(provider =>
        {
            var cosmosClient = provider.GetRequiredService<CosmosClient>();
            return cosmosClient.GetContainer(settings.Database, settings.Containers.Transcripts);
        });

        return services;
    }
}
