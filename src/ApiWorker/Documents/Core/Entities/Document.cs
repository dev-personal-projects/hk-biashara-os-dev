//base: Id, BusinessId, Number, Status, Type, BlobUrl, etc.
// src/ApiWorker/Documents/Entities/Document.cs
using System;
using System.Collections.Generic;

namespace ApiWorker.Documents.Entities;

/// <summary>
/// Base for all business documents. Every document
/// belongs to a Business and has a creator User.
/// </summary>
public abstract class Document
{
    public Guid Id { get; set; }

    // Ownership / scope
    public Guid BusinessId { get; set; }         // FK -> Business.Id
    public Guid CreatedByUserId { get; set; }    // FK -> AppUser.Id
    public Guid? TemplateId { get; set; }

    // Identity & lifecycle
    public DocumentType Type { get; set; }
    public string Number { get; set; } = string.Empty; // unique per (Business,Type)
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public int Version { get; set; } = 1;

    // Money
    public string Currency { get; set; } = "KES"; // ISO-4217
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Timing
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DueAt { get; set; }

    // Storage locations
    public string? DocxBlobUrl { get; set; }
    public string? PdfBlobUrl { get; set; }
    public string? PreviewBlobUrl { get; set; }

    // Optional pointer to NoSQL (Cosmos) for the rendered JSON snapshot
    public string? CosmosId { get; set; }

    // Theme snapshot & signature metadata
    public string? AppliedThemeJson { get; set; }
    public string? SignatureBlobUrl { get; set; }
    public string? SignedBy { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public string? SignatureNotes { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Concurrency (SQL Server rowversion/timestamp)
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation (optional to keep compile-time independence from Auth module)
    // public Business Business { get; set; } = default!;
    // public AppUser CreatedBy { get; set; } = default!;
}
