using ApiWorker.Speech.DTOs;

namespace ApiWorker.Speech.Interfaces;

public interface ISpeechToTextService
{
    Task<TranscriptionResult> TranscribeAsync(
        Stream audio,
        string? locale = null,
        CancellationToken ct = default
    );

    Task<TranscriptionResult> TranscribeFileAsync(
        string localFilePath,
        string? locale = null,
        CancellationToken ct = default
    );
}
