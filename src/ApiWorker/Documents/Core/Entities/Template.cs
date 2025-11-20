//Id, Type, Name, Version, BlobPath, Fields (JSON)// src/ApiWorker/Documents/Entities/Template.cs
using System;

namespace ApiWorker.Documents.Entities;

/// <summary>
/// Versioned DOCX template per document type. Optionally business-scoped.
/// FieldsJson stores array of TemplateField as JSON.
/// </summary>
public sealed class Template
{
    public Guid Id { get; set; }
    public Guid? BusinessId { get; set; }     // null => global template
    public DocumentType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string BlobPath { get; set; } = string.Empty; // container/key for DOCX
    public string? FieldsJson { get; set; }              // serialized TemplateField[]
    public bool IsDefault { get; set; } = false;
    public string? ThemeJson { get; set; }
    public string? PreviewBlobUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
