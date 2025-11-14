using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Documents.Settings;

/// <summary>
/// Blob storage containers/paths for templates and rendered documents.
/// Keep URLs out of code; derive public links from these settings.
/// </summary>
public sealed class TemplateStorageSettings
{
    /// <summary>Container for DOCX templates.</summary>
    [Required, MinLength(1)]
    public string TemplatesContainer { get; init; } = "doc-templates";

    /// <summary>Container for final/generated documents (PDF/DOCX).</summary>
    [Required, MinLength(1)]
    public string DocumentsContainer { get; init; } = "docs";

    /// <summary>Optional container for thumbnails/previews.</summary>
    public string? PreviewsContainer { get; init; } = "doc-previews";

    /// <summary>
    /// Optional CDN/public base URL (e.g., https://cdn.example.com).
    /// If set, sharing links should be built with this base instead of raw blob URIs.
    /// </summary>
    public string? CdnBaseUrl { get; init; }

    /// <summary>
    /// Path format inside a container.
    /// Tokens: {businessId} {type} {yyyy} {MM} {fileName}
    /// Example: {businessId}/{type}/{yyyy}/{MM}/{fileName}
    /// </summary>
    [Required, MinLength(8)]
    public string PathFormat { get; init; } = "{businessId}/{type}/{yyyy}/{MM}/{fileName}";
}
