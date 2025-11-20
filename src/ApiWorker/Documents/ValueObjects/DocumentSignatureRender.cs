namespace ApiWorker.Documents.ValueObjects;

/// <summary>
/// Signature metadata + image payload passed into renderers.
/// ImageBytes can be null when document is not yet signed.
/// </summary>
public sealed class DocumentSignatureRender
{
    public byte[]? ImageBytes { get; init; }
    public string? SignedBy { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public string? Notes { get; init; }

    public bool HasSignature => (ImageBytes != null && ImageBytes.Length > 0) || !string.IsNullOrWhiteSpace(SignedBy);
}

