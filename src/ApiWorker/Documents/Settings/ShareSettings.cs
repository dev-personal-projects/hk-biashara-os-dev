using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Documents.Settings;

/// <summary>
/// Settings for sharing documents (WhatsApp, public links, QR).
/// </summary>
public sealed class ShareSettings
{
    /// <summary>
    /// Public base URL used to build human-friendly links to documents
    /// (e.g., a short domain or CDN). Optional.
    /// </summary>
    public string? PublicFileBaseUrl { get; init; }

    /// <summary>WhatsApp Business Cloud API credentials.</summary>
    [Required]
    public WhatsAppOptions WhatsApp { get; init; } = new();

    public sealed class WhatsAppOptions
    {
        /// <summary>Meta WhatsApp Business phone_number_id.</summary>
        [Required, MinLength(5)]
        public string PhoneNumberId { get; init; } = string.Empty;

        /// <summary>Meta Graph API access token with permissions to send messages.</summary>
        [Required, MinLength(10)]
        public string AccessToken { get; init; } = string.Empty;

        /// <summary>Graph API base version (keep configurable for upgrades).</summary>
        [Required]
        public string ApiBase { get; init; } = "https://graph.facebook.com/v20.0";

        /// <summary>When true, don't actually call the API (useful for dev/test).</summary>
        public bool SandboxMode { get; init; } = false;
    }
}
