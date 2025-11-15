using ApiWorker.Speech.DTOs;

namespace ApiWorker.Speech.Storage;

/// <summary>
/// Repository interface for storing and retrieving speech transcription records.
/// Abstracts the underlying storage mechanism (Cosmos DB) for testability.
/// </summary>
public interface ITranscriptionStore
{
    /// <summary>
    /// Saves a transcription record to storage.
    /// </summary>
    /// <param name="record">The transcription record to save</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The ID of the saved record</returns>
    Task<string> SaveAsync(TranscriptionRecord record, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific transcription record by ID and business.
    /// </summary>
    /// <param name="id">The record ID</param>
    /// <param name="businessId">The business ID (partition key)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The transcription record, or null if not found</returns>
    Task<TranscriptionRecord?> GetAsync(string id, Guid businessId, CancellationToken ct = default);

    /// <summary>
    /// Lists all transcription records for a specific business.
    /// Uses async enumerable for efficient streaming of large result sets.
    /// </summary>
    /// <param name="businessId">The business ID to filter by</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Async enumerable of transcription records</returns>
    IAsyncEnumerable<TranscriptionRecord> ListByBusinessAsync(Guid businessId, CancellationToken ct = default);
}
