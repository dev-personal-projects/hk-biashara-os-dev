using System.Runtime.CompilerServices;
using ApiWorker.Speech.DTOs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ApiWorker.Speech.Storage;

/// <summary>
/// Cosmos DB implementation of transcription storage.
/// Uses partition key (/businessId) for efficient tenant isolation.
/// </summary>
public sealed class CosmosTranscriptionStore : ITranscriptionStore
{
    private readonly Container _container;

    /// <summary>
    /// Initializes the store with a Cosmos DB container.
    /// Container is injected as singleton from DI.
    /// </summary>
    public CosmosTranscriptionStore(Container container) => _container = container;

    /// <summary>
    /// Saves a transcription record to Cosmos DB.
    /// Uses CreateItemAsync for new records (throws if duplicate ID exists).
    /// </summary>
    public async Task<string> SaveAsync(TranscriptionRecord record, CancellationToken ct = default)
    {
        // Partition key must match the container's partition key path (/businessId)
        var partitionKey = new PartitionKey(record.BusinessId);
        
        // CreateItemAsync is more efficient than UpsertItemAsync when you know it's a new record
        var response = await _container.CreateItemAsync(record, partitionKey, cancellationToken: ct);
        
        // Return the ID for reference (useful for linking to other systems)
        return response.Resource.Id;
    }

    /// <summary>
    /// Retrieves a single transcription record by ID.
    /// Requires both ID and partition key for efficient point-read operation.
    /// </summary>
    public async Task<TranscriptionRecord?> GetAsync(string id, Guid businessId, CancellationToken ct = default)
    {
        try
        {
            // Point-read is the most efficient Cosmos DB operation (1 RU)
            // Requires both document ID and partition key value
            var response = await _container.ReadItemAsync<TranscriptionRecord>(
                id, 
                new PartitionKey(businessId.ToString()), 
                cancellationToken: ct);
            
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Return null instead of throwing for not found (common pattern)
            return null;
        }
    }

    /// <summary>
    /// Lists all transcription records for a business using async streaming.
    /// Query is partition-targeted for efficiency (only scans one partition).
    /// </summary>
    public async IAsyncEnumerable<TranscriptionRecord> ListByBusinessAsync(
        Guid businessId, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Use LINQ query for type-safe querying
        var queryable = _container.GetItemLinqQueryable<TranscriptionRecord>(
            requestOptions: new QueryRequestOptions 
            { 
                // Partition key targeting ensures query only scans one partition
                PartitionKey = new PartitionKey(businessId.ToString()),
                // Optional: Limit max items per page for memory efficiency
                MaxItemCount = 100
            });

        // Convert to feed iterator for pagination support
        using var feedIterator = queryable.ToFeedIterator();
        
        // Stream results page by page to avoid loading everything into memory
        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(ct);
            
            // Yield each item for efficient streaming to caller
            foreach (var item in response)
            {
                yield return item;
            }
        }
    }
}
