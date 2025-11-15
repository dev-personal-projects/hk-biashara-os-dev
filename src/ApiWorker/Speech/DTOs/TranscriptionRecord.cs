using System.Text.Json.Serialization;

namespace ApiWorker.Speech.DTOs;

/// <summary>
/// Represents a speech transcription record stored in Cosmos DB.
/// Partitioned by businessId for efficient tenant isolation and queries.
/// Keep flat structure for optimal Cosmos DB performance.
/// </summary>
public sealed class TranscriptionRecord
{
    /// <summary>
    /// Unique identifier for this transcription (Cosmos DB document id)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Business/tenant identifier (partition key for Cosmos DB)
    /// All queries should include this for efficient partition-targeted operations
    /// </summary>
    [JsonPropertyName("businessId")]
    public string BusinessId { get; set; } = string.Empty;

    /// <summary>
    /// User who created this transcription
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Language/locale code (e.g., "en-KE" for English-Kenya, "sw-KE" for Swahili-Kenya)
    /// </summary>
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = "en-KE";

    /// <summary>
    /// The transcribed text from speech recognition
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score from speech recognition (0.0 to 1.0)
    /// Higher values indicate more confident transcription
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.0;

    /// <summary>
    /// Optional URL to the original audio file in blob storage
    /// Useful for audit trail and re-processing if needed
    /// </summary>
    [JsonPropertyName("audioBlobUrl")]
    public string? AudioBlobUrl { get; set; }

    /// <summary>
    /// Timestamp when this transcription was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Source of the transcription (e.g., "mobile", "web", "bot")
    /// Useful for analytics and debugging
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "mobile";
}
