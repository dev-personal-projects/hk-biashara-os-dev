using ApiWorker.Speech.DTOs;
using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Settings;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;

namespace ApiWorker.Speech.Services;

public sealed class AzureSpeechToTextService : ISpeechToTextService
{
    private readonly SpeechSettings _cfg;
    private readonly ILogger<AzureSpeechToTextService> _logger;

    public AzureSpeechToTextService(IOptions<SpeechSettings> cfg, ILogger<AzureSpeechToTextService> logger)
    {
        _cfg = cfg.Value;
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audio, string? locale = null, CancellationToken ct = default)
    {
        var pushStream = AudioInputStream.CreatePushStream();

        _ = Task.Run(async () =>
        {
            var buffer = new byte[8192];
            int read;
            while ((read = await audio.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                pushStream.Write(buffer.AsSpan(0, read).ToArray());
                if (ct.IsCancellationRequested) break;
            }
            pushStream.Close();
        }, ct);

        using var audioConfig = AudioConfig.FromStreamInput(pushStream);
        return await RecognizeOnceAsync(audioConfig, locale ?? _cfg.DefaultLocale, ct);
    }

    public async Task<TranscriptionResult> TranscribeFileAsync(string localFilePath, string? locale = null, CancellationToken ct = default)
    {
        using var audioConfig = AudioConfig.FromWavFileInput(localFilePath);
        return await RecognizeOnceAsync(audioConfig, locale ?? _cfg.DefaultLocale, ct);
    }

    private async Task<TranscriptionResult> RecognizeOnceAsync(AudioConfig audioConfig, string locale, CancellationToken ct)
    {
        var speechConfig = SpeechConfig.FromSubscription(_cfg.Key, _cfg.Region);
        speechConfig.SpeechRecognitionLanguage = locale;

        if (_cfg.UseNumeralNormalization)
        {
            speechConfig.EnableDictation();
        }

        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                return new TranscriptionResult(true, result.Text, locale, Confidence: 0.0);
            case ResultReason.NoMatch:
                _logger.LogWarning("Speech recognition: no match");
                return new TranscriptionResult(false, string.Empty, locale, Error: "No speech recognized");
            case ResultReason.Canceled:
                var cancel = CancellationDetails.FromResult(result);
                _logger.LogError("Speech canceled: {Reason} {ErrorCode} {Details}", cancel.Reason, cancel.ErrorCode, cancel.ErrorDetails);
                return new TranscriptionResult(false, string.Empty, locale, Error: cancel.ErrorDetails ?? "Canceled");
            default:
                return new TranscriptionResult(false, string.Empty, locale, Error: "Unknown result");
        }
    }
}
