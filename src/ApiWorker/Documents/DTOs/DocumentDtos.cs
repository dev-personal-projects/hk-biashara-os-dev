using System.ComponentModel.DataAnnotations;
using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.DTOs;

// ===== COMMON DOCUMENT RESPONSES =====

/// <summary>
/// Standard response for document operations (create, update, share).
/// Provides consistent structure for mobile app to handle success/error cases.
/// </summary>
public sealed class DocumentResponse
{
    /// <summary>Indicates if the operation succeeded</summary>
    public bool Success { get; init; }

    /// <summary>User-friendly message (e.g., "Invoice created successfully")</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Document ID (null if operation failed)</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Document number (e.g., "INV-202401-0001")</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>URLs to rendered documents (DOCX and PDF)</summary>
    public DocumentUrls? Urls { get; init; }

    /// <summary>Signature info if the document has been signed.</summary>
    public DocumentSignatureDto? Signature { get; init; }
}

/// <summary>
/// URLs to access rendered documents.
/// Mobile app can download or share these URLs.
/// </summary>
public sealed class DocumentUrls
{
    /// <summary>URL to DOCX file (editable format)</summary>
    public string? DocxUrl { get; init; }

    /// <summary>URL to PDF file (final format for sharing)</summary>
    public string? PdfUrl { get; init; }

    /// <summary>Optional preview/thumbnail URL</summary>
    public string? PreviewUrl { get; init; }
}

// ===== DOCUMENT LISTING =====

/// <summary>
/// Summary of a document for list views in mobile app.
/// Lightweight DTO without full details.
/// </summary>
public sealed class DocumentSummary
{
    public Guid Id { get; init; }
    public DocumentType Type { get; init; }
    public string Number { get; init; } = string.Empty;
    public DocumentStatus Status { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }

    /// <summary>Customer/recipient name for display</summary>
    public string? RecipientName { get; init; }

    /// <summary>Quick access to PDF for sharing</summary>
    public string? PdfUrl { get; init; }
}

/// <summary>
/// Request to list documents with filtering and pagination.
/// Mobile app uses this for document history screens.
/// </summary>
public sealed class ListDocumentsRequest
{
    /// <summary>Filter by document type (optional)</summary>
    public DocumentType? Type { get; init; }

    /// <summary>Filter by status (optional)</summary>
    public DocumentStatus? Status { get; init; }

    /// <summary>Filter by date range - start (optional)</summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>Filter by date range - end (optional)</summary>
    public DateTimeOffset? ToDate { get; init; }

    /// <summary>Search by document number or customer name (optional)</summary>
    [MaxLength(100)]
    public string? SearchTerm { get; init; }

    /// <summary>Page number (1-based)</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Items per page</summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Paginated response for document lists.
/// Mobile app uses this for infinite scroll or pagination.
/// </summary>
public sealed class ListDocumentsResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<DocumentSummary> Documents { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>
/// Theme definition sent from/to the client to customize document colors and fonts.
/// </summary>
public sealed class DocumentThemeDto
{
    public string PrimaryColor { get; init; } = "#111827";
    public string SecondaryColor { get; init; } = "#1F2937";
    public string AccentColor { get; init; } = "#F97316";
    public string FontFamily { get; init; } = "Poppins";
}

/// <summary>
/// Signature metadata returned to clients.
/// </summary>
public sealed class DocumentSignatureDto
{
    public bool IsSigned { get; init; }
    public string? SignedBy { get; init; }
    public DateTimeOffset? SignedAt { get; init; }
    public string? SignatureUrl { get; init; }
    public string? Notes { get; init; }
}
