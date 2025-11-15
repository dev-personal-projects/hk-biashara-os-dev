namespace ApiWorker.Speech.DTOs;

public sealed record TranscriptionResult(
    bool Success,
    string Text,
    string Locale,
    double Confidence = 0.0,
    string? Error = null
);
