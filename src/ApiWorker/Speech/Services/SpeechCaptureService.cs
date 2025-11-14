using ApiWorker.Speech.DTOs;
using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Storage;

namespace ApiWorker.Speech.Services;

/// <summary>
/// Orchestrates speech transcription and storage.
/// Flow: Audio → Azure Speech SDK → TranscriptionResult → Cosmos DB → TranscriptionRecord
/// </summary>
public sealed class SpeechCaptureService : ISpeechCaptureService
{
    private readonly ISpeechToTextService _stt;
    private readonly ITranscriptionStore _store;
    private readonly ILogger<SpeechCaptureService> _logger;

    public SpeechCaptureService(
        ISpeechToTextService stt,
        ITranscriptionStore store,
        ILogger<SpeechCaptureService> logger)
    {
        _stt = stt;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Captures audio stream, transcribes it, and stores the result.
    /// </summary>
    public async Task<(TranscriptionRecord Record, bool Success)> CaptureAsync(
        Stream audio,
        TranscriptionContext context,
        CancellationToken ct = default)
    {
        // Step 1: Transcribe audio using Azure Speech SDK
        var transcriptionResult = await _stt.TranscribeAsync(audio, context.Locale, ct);

        // Step 2: Map transcription result to storage record
        var record = new TranscriptionRecord
        {
            BusinessId = context.BusinessId.ToString(),
            UserId = context.UserId.ToString(),
            Locale = transcriptionResult.Locale,
            Text = transcriptionResult.Text,
            Confidence = transcriptionResult.Confidence,
            AudioBlobUrl = context.AudioBlobUrl,
            Source = context.Source
        };

        // Step 3: Persist to Cosmos DB for audit trail and analytics
        try
        {
            await _store.SaveAsync(record, ct);
            _logger.LogInformation(
                "Transcription saved: Business={BusinessId}, User={UserId}, Confidence={Confidence}", 
                context.BusinessId, context.UserId, transcriptionResult.Confidence);
            
            return (record, transcriptionResult.Success);
        }
        catch (Exception ex)
        {
            // Log error but still return the transcription result
            // This allows the caller to use the transcription even if storage fails
            _logger.LogError(ex, 
                "Failed to persist transcription for business {BusinessId}", 
                context.BusinessId);
            
            return (record, false);
        }
    }

    /// <summary>
    /// Captures audio from file, transcribes it, and stores the result.
    /// Convenience method that opens the file and delegates to CaptureAsync.
    /// </summary>
    public async Task<(TranscriptionRecord Record, bool Success)> CaptureFileAsync(
        string localFilePath,
        TranscriptionContext context,
        CancellationToken ct = default)
    {
        // Open file as stream and delegate to stream-based method
        // Using 'using' ensures file handle is properly closed
        using var audioStream = File.OpenRead(localFilePath);
        return await CaptureAsync(audioStream, context, ct);
    }
}
