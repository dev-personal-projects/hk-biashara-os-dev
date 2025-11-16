using Azure.Storage.Blobs;

namespace ApiWorker.Configuration.Extensions;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage")
            ?? throw new InvalidOperationException("BlobStorage connection string not found in Key Vault.");
        
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddScoped<ApiWorker.Storage.IBlobStorageService, ApiWorker.Storage.BlobStorageService>();
        
        return services;
    }
}
