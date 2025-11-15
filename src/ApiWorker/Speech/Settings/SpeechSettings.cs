namespace ApiWorker.Speech.Settings;

public sealed class SpeechSettings
{
    public string Provider { get; set; } = "AzureSpeech";
    public string Region { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = "en-KE";
    public string[]? Locales { get; set; }
    public bool UseNumeralNormalization { get; set; } = true;
}
