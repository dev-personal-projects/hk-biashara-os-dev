using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Speech.Settings;

/// <summary>
/// Azure Speech configuration for voice-to-text transcription.
/// Used across all voice features (invoices, receipts, commands).
/// </summary>
public sealed class VoiceSettings
{
    /// <summary>
    /// Speech provider identifier (currently "AzureSpeech").
    /// Kept extensible for future providers (Google, AWS, etc.).
    /// </summary>
    [Required]
    public string Provider { get; init; } = "AzureSpeech";

    /// <summary>
    /// Azure region where Speech service is deployed (e.g., "eastus", "westeurope").
    /// Must match your Azure Speech resource location.
    /// </summary>
    [Required, MinLength(3)]    
    public string Region { get; init; } = string.Empty;

    /// <summary>
    /// Azure Speech subscription key (primary or secondary).
    /// Keep this secret - never commit to source control.
    /// </summary>
    [Required, MinLength(8)]
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Default speech locale when client doesn't specify one.
    /// Format: language-COUNTRY (e.g., "en-KE" for English-Kenya).
    /// </summary>
    [Required]
    public string DefaultLocale { get; init; } = "en-KE";

    /// <summary>
    /// Whitelisted locales that clients can request.
    /// Prevents clients from requesting unsupported languages.
    /// </summary>
    [Required, MinLength(1)]
    public string[] Locales { get; init; } = new[] { "en-KE", "sw-KE" };

    /// <summary>
    /// Enable numeral normalization (e.g., "one thousand" → "1000").
    /// Useful for invoice amounts and quantities.
    /// </summary>
    public bool UseNumeralNormalization { get; init; } = true;

    /// <summary>
    /// Optional phrase list for domain-specific terms.
    /// Boosts recognition accuracy for product names, brands, etc.
    /// Example: ["M-Pesa", "Unga", "Mama Mboga"]
    /// </summary>
    public string[] PhraseList { get; init; } = Array.Empty<string>();
}
