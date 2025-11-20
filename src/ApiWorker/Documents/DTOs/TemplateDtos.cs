using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.DTOs;

/// <summary>
/// Template information returned to clients.
/// </summary>
public sealed class TemplateDto
{
    public Guid Id { get; init; }
    public DocumentType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; }
    public string BlobUrl { get; init; } = string.Empty;
    public string? PreviewUrl { get; init; }
    public DocumentThemeDto? Theme { get; init; }
    public bool IsDefault { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Request to list templates (optional type filter).
/// </summary>
public sealed class ListTemplatesRequest
{
    /// <summary>Filter by document type (optional)</summary>
    public DocumentType? Type { get; init; }
}

/// <summary>
/// Response containing list of templates.
/// </summary>
public sealed class ListTemplatesResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<TemplateDto> Templates { get; init; } = new();
}

/// <summary>
/// Response containing single template details.
/// </summary>
public sealed class GetTemplateResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public TemplateDto? Template { get; init; }
}

