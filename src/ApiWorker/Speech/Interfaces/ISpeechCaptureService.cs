using ApiWorker.Speech.DTOs;

namespace ApiWorker.Speech.Interfaces;

/// <summary>
/// High-level service that orchestrates speech-to-text conversion and storage.
/// Combines ISpeechToTextService (Azure Speech SDK) with ITranscriptionStore (Cosmos DB).
/// </summary>
public interface ISpeechCaptureService
{
    /// <summary>
    /// Transcribes audio from a stream and stores the result in Cosmos DB.
    /// Use this for real-time audio streams from mobile/web clients.
    /// </summary>
    /// <param name="audio">Audio stream (WAV, MP3, or other supported format)</param>
    /// <param name="context">Context information (business, user, locale, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (TranscriptionRecord, Success flag)</returns>
    Task<(TranscriptionRecord Record, bool Success)> CaptureAsync(
        Stream audio,
        TranscriptionContext context,
        CancellationToken ct = default
    );

    /// <summary>
    /// Transcribes audio from a local file and stores the result in Cosmos DB.
    /// Use this for batch processing or when audio is already saved to disk.
    /// </summary>
    /// <param name="localFilePath">Path to audio file on local filesystem</param>
    /// <param name="context">Context information (business, user, locale, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (TranscriptionRecord, Success flag)</returns>
    Task<(TranscriptionRecord Record, bool Success)> CaptureFileAsync(
        string localFilePath,
        TranscriptionContext context,
        CancellationToken ct = default
    );
}
