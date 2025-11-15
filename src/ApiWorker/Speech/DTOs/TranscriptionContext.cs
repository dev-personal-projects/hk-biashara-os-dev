namespace ApiWorker.Speech.DTOs;

/// <summary>
/// Context information for a speech transcription request.
/// Provides tenant isolation and attribution metadata.
/// </summary>
/// <param name="BusinessId">The business/tenant this transcription belongs to (used for partitioning)</param>
/// <param name="UserId">The user who initiated the transcription</param>
/// <param name="Locale">Language/locale code (e.g., "en-KE", "sw-KE")</param>
/// <param name="AudioBlobUrl">Optional URL to the audio file in blob storage for audit trail</param>
/// <param name="Source">Source of the transcription (e.g., "mobile", "web", "bot")</param>
public sealed record TranscriptionContext(
    Guid BusinessId,
    Guid UserId,
    string? Locale = null,
    string? AudioBlobUrl = null,
    string Source = "mobile"
);
