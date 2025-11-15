namespace ApiWorker.Cosmos.Settings;

/// <summary>
/// Configuration for Azure Cosmos DB NoSQL API.
/// Used for storing high-volume, schema-flexible data like speech transcripts.
/// </summary>
public sealed class CosmosSettings
{
    /// <summary>
    /// Cosmos DB account endpoint URL (e.g., https://your-account.documents.azure.com:443/)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Primary or secondary access key for authentication
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Database name - logical grouping of containers
    /// </summary>
    public string Database { get; set; } = "biashara";

    /// <summary>
    /// Auto-create database and containers if they don't exist (useful for dev/test)
    /// Set to false in production for better control
    /// </summary>
    public bool CreateIfNotExists { get; set; } = true;

    /// <summary>
    /// Container names for different data types
    /// </summary>
    public CosmosContainers Containers { get; set; } = new();
}

public sealed class CosmosContainers
{
    /// <summary>
    /// Container for storing speech transcription records
    /// Partitioned by /businessId for efficient tenant isolation
    /// </summary>
    public string Transcripts { get; set; } = "speech-transcripts";
}
