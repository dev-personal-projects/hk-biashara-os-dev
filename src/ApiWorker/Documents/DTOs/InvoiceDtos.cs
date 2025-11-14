using System.ComponentModel.DataAnnotations;

namespace ApiWorker.Documents.DTOs;

// ===== VOICE INVOICE CREATION =====

/// <summary>
/// Request to create invoice from voice input.
/// Mobile app records audio, optionally transcribes locally, then sends to API.
/// Flow: User speaks → Mobile transcribes → API extracts fields → Creates invoice
/// </summary>
public sealed class CreateInvoiceFromVoiceRequest
{
    /// <summary>Business ID (from authenticated user context)</summary>
    [Required]
    public Guid BusinessId { get; init; }

    /// <summary>
    /// Pre-transcribed text from mobile app (if offline transcription was used).
    /// If provided, API skips Azure Speech SDK and uses this directly.
    /// </summary>
    [MaxLength(2000)]
    public string? TranscriptText { get; init; }

    /// <summary>
    /// URL to audio file in blob storage (if mobile uploaded audio first).
    /// API will transcribe using Azure Speech SDK if TranscriptText is not provided.
    /// </summary>
    [MaxLength(512)]
    public string? AudioBlobUrl { get; init; }

    /// <summary>Locale for transcription (e.g., "en-KE", "sw-KE")</summary>
    [Required]
    public string Locale { get; init; } = "en-KE";

    /// <summary>
    /// Optional template ID to use for rendering.
    /// If not provided, uses business default template.
    /// </summary>
    public Guid? TemplateId { get; init; }
}

// ===== MANUAL INVOICE CREATION =====

/// <summary>
/// Request to create invoice manually (user fills form in mobile app).
/// Provides full control over all invoice fields.
/// </summary>
public sealed class CreateInvoiceManuallyRequest
{
    /// <summary>Business ID (from authenticated user context)</summary>
    [Required]
    public Guid BusinessId { get; init; }

    /// <summary>Customer information</summary>
    [Required]
    public CustomerDto Customer { get; init; } = new();

    /// <summary>Invoice line items (products/services)</summary>
    [Required, MinLength(1)]
    public List<InvoiceLineDto> Lines { get; init; } = new();

    /// <summary>Currency code (e.g., "KES", "USD")</summary>
    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "KES";

    /// <summary>Issue date (defaults to now if not provided)</summary>
    public DateTimeOffset? IssuedAt { get; init; }

    /// <summary>Due date (optional)</summary>
    public DateTimeOffset? DueAt { get; init; }

    /// <summary>Optional notes/terms (e.g., "Payment due within 30 days")</summary>
    [MaxLength(1000)]
    public string? Notes { get; init; }

    /// <summary>Optional reference number (e.g., PO number)</summary>
    [MaxLength(64)]
    public string? Reference { get; init; }

    /// <summary>
    /// Optional template ID to use for rendering.
    /// If not provided, uses business default template.
    /// </summary>
    public Guid? TemplateId { get; init; }
}

// ===== INVOICE UPDATE (EDIT) =====

/// <summary>
/// Request to update an existing invoice (only allowed if status is Draft).
/// Mobile app uses this when user edits a draft invoice.
/// </summary>
public sealed class UpdateInvoiceRequest
{
    /// <summary>Invoice ID to update</summary>
    [Required]
    public Guid InvoiceId { get; init; }

    /// <summary>Updated customer information (optional)</summary>
    public CustomerDto? Customer { get; init; }

    /// <summary>Updated line items (optional - replaces all existing lines)</summary>
    public List<InvoiceLineDto>? Lines { get; init; }

    /// <summary>Updated due date (optional)</summary>
    public DateTimeOffset? DueAt { get; init; }

    /// <summary>Updated notes (optional)</summary>
    [MaxLength(1000)]
    public string? Notes { get; init; }

    /// <summary>Updated reference (optional)</summary>
    [MaxLength(64)]
    public string? Reference { get; init; }
}

// ===== SUPPORTING DTOs =====

/// <summary>
/// Customer information for invoices.
/// Minimal required fields for mobile-first experience.
/// </summary>
public sealed class CustomerDto
{
    /// <summary>Customer name (required)</summary>
    [Required, MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Customer phone (optional but recommended for WhatsApp sharing)</summary>
    [MaxLength(32)]
    public string? Phone { get; init; }

    /// <summary>Customer email (optional)</summary>
    [EmailAddress, MaxLength(128)]
    public string? Email { get; init; }

    /// <summary>Billing address line 1 (optional)</summary>
    [MaxLength(256)]
    public string? AddressLine1 { get; init; }

    /// <summary>Billing address line 2 (optional)</summary>
    [MaxLength(256)]
    public string? AddressLine2 { get; init; }

    /// <summary>City (optional)</summary>
    [MaxLength(64)]
    public string? City { get; init; }

    /// <summary>Country (optional)</summary>
    [MaxLength(64)]
    public string? Country { get; init; }
}

/// <summary>
/// Single line item on an invoice.
/// Represents a product or service being billed.
/// </summary>
public sealed class InvoiceLineDto
{
    /// <summary>Product/service name (e.g., "Maize Flour 2kg")</summary>
    [Required, MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional description (e.g., "Premium quality")</summary>
    [MaxLength(512)]
    public string? Description { get; init; }

    /// <summary>Quantity (e.g., 3.5 for 3.5 bags)</summary>
    [Required, Range(0.001, 999999)]
    public decimal Quantity { get; init; } = 1m;

    /// <summary>Price per unit (e.g., 180.00 for KES 180)</summary>
    [Required, Range(0, 999999999)]
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Tax rate for this line (e.g., 0.16 for 16% VAT).
    /// If not provided, uses business default or 0.
    /// </summary>
    [Range(0, 1)]
    public decimal? TaxRate { get; init; }
}

// ===== INVOICE DETAIL RESPONSE =====

/// <summary>
/// Full invoice details for viewing/editing in mobile app.
/// Includes all fields and computed totals.
/// </summary>
public sealed class InvoiceDetailResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public InvoiceDto? Invoice { get; init; }
}

/// <summary>
/// Complete invoice data transfer object.
/// Used for displaying invoice details in mobile app.
/// </summary>
public sealed class InvoiceDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;

    public CustomerDto Customer { get; init; } = new();
    public List<InvoiceLineDto> Lines { get; init; } = new();

    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal Total { get; init; }

    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }

    public string? Notes { get; init; }
    public string? Reference { get; init; }

    public DocumentUrls? Urls { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
