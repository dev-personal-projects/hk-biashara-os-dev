using FluentValidation;
using ApiWorker.Documents.DTOs;

namespace ApiWorker.Documents.Validators;

// ===== VOICE DOCUMENT VALIDATOR =====

/// <summary>
/// Validates voice document creation requests.
/// Ensures either transcript text or audio URL is provided (not both empty).
/// </summary>
public sealed class CreateDocumentFromVoiceRequestValidator : AbstractValidator<CreateDocumentFromVoiceRequest>
{
    public CreateDocumentFromVoiceRequestValidator()
    {
        // Business ID is required (user must be authenticated)
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required");

        // Document type must be valid
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Document type must be Invoice, Receipt, or Quotation");

        // Locale must be valid (en-KE or sw-KE)
        RuleFor(x => x.Locale)
            .NotEmpty()
            .Must(locale => locale == "en-KE" || locale == "sw-KE")
            .WithMessage("Locale must be 'en-KE' or 'sw-KE'");

        // Either transcript text OR audio URL must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.TranscriptText) || !string.IsNullOrWhiteSpace(x.AudioBlobUrl))
            .WithMessage("Either TranscriptText or AudioBlobUrl must be provided");

        // If transcript is provided, it should have reasonable length
        When(x => !string.IsNullOrWhiteSpace(x.TranscriptText), () =>
        {
            RuleFor(x => x.TranscriptText)
                .MinimumLength(10)
                .WithMessage("Transcript text is too short (minimum 10 characters)");
        });
    }
}

// ===== MANUAL DOCUMENT VALIDATOR =====

/// <summary>
/// Validates manual document creation requests.
/// Ensures all required fields are present and business rules are met.
/// </summary>
public sealed class CreateDocumentManuallyRequestValidator : AbstractValidator<CreateDocumentManuallyRequest>
{
    public CreateDocumentManuallyRequestValidator()
    {
        // Business ID is required
        RuleFor(x => x.BusinessId)
            .NotEmpty()
            .WithMessage("Business ID is required");

        // Customer is required and must be valid
        RuleFor(x => x.Customer)
            .NotNull()
            .WithMessage("Customer information is required")
            .SetValidator(new CustomerDtoValidator());

        // At least one line item is required
        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("At least one line item is required");

        // Each line item must be valid
        RuleForEach(x => x.Lines)
            .SetValidator(new DocumentLineDtoValidator());

        // Document type must be valid
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Document type must be Invoice, Receipt, or Quotation");

        // Currency must be valid ISO code
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code (e.g., KES, USD)");

        // Due date must be after issue date (if both provided)
        When(x => x.IssuedAt.HasValue && x.DueAt.HasValue, () =>
        {
            RuleFor(x => x.DueAt)
                .GreaterThan(x => x.IssuedAt!.Value)
                .WithMessage("Due date must be after issue date");
        });

        // Notes should not be excessively long
        When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("Notes cannot exceed 1000 characters");
        });
    }
}

// ===== UPDATE DOCUMENT VALIDATOR =====

/// <summary>
/// Validates document update requests.
/// At least one field must be provided for update.
/// </summary>
public sealed class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        // Document ID is required
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required");

        // At least one field must be provided for update
        RuleFor(x => x)
            .Must(x => x.Customer != null || x.Lines != null || x.DueAt.HasValue ||
                      !string.IsNullOrWhiteSpace(x.Notes) || !string.IsNullOrWhiteSpace(x.Reference))
            .WithMessage("At least one field must be provided for update");

        // If customer is provided, validate it
        When(x => x.Customer != null, () =>
        {
            RuleFor(x => x.Customer)
                .SetValidator(new CustomerDtoValidator()!);
        });

        // If lines are provided, validate them
        When(x => x.Lines != null && x.Lines.Any(), () =>
        {
            RuleForEach(x => x.Lines)
                .SetValidator(new DocumentLineDtoValidator());
        });
    }
}

// ===== CUSTOMER VALIDATOR =====

/// <summary>
/// Validates customer information.
/// Name is required, other fields are optional but validated if provided.
/// </summary>
public sealed class CustomerDtoValidator : AbstractValidator<CustomerDto>
{
    public CustomerDtoValidator()
    {
        // Customer name is required
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Customer name is required")
            .MaximumLength(128)
            .WithMessage("Customer name cannot exceed 128 characters");

        // Phone format validation (if provided)
        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
        {
            RuleFor(x => x.Phone)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Phone number must be in international format (e.g., +254712345678)");
        });

        // Email validation (if provided)
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email address format");
        });
    }
}

// ===== DOCUMENT LINE VALIDATOR =====

/// <summary>
/// Validates document line items.
/// Ensures quantities and prices are positive and reasonable.
/// </summary>
public sealed class DocumentLineDtoValidator : AbstractValidator<DocumentLineDto>
{
    public DocumentLineDtoValidator()
    {
        // Product/service name is required
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Line item name is required")
            .MaximumLength(128)
            .WithMessage("Line item name cannot exceed 128 characters");

        // Quantity must be positive
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(999999)
            .WithMessage("Quantity cannot exceed 999,999");

        // Unit price must be non-negative (free items allowed)
        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Unit price cannot be negative")
            .LessThanOrEqualTo(999999999)
            .WithMessage("Unit price is too large");

        // Tax rate must be between 0 and 100% (if provided)
        When(x => x.TaxRate.HasValue, () =>
        {
            RuleFor(x => x.TaxRate)
                .InclusiveBetween(0m, 1m)
                .WithMessage("Tax rate must be between 0 and 1 (e.g., 0.16 for 16%)");
        });

        // Line total should make sense (quantity * price should not overflow)
        RuleFor(x => x)
            .Must(x => x.Quantity * x.UnitPrice <= 999999999)
            .WithMessage("Line total is too large (quantity × unit price exceeds maximum)");
    }
}

// ===== LIST DOCUMENTS VALIDATOR =====

/// <summary>
/// Validates document listing requests.
/// Ensures pagination parameters are reasonable.
/// </summary>
public sealed class ListDocumentsRequestValidator : AbstractValidator<ListDocumentsRequest>
{
    public ListDocumentsRequestValidator()
    {
        // Page must be positive
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        // Page size must be reasonable (1-100)
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        // Date range validation (FromDate must be before ToDate)
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.ToDate)
                .GreaterThan(x => x.FromDate!.Value)
                .WithMessage("ToDate must be after FromDate");
        });

        // Search term should not be too long
        When(x => !string.IsNullOrWhiteSpace(x.SearchTerm), () =>
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100)
                .WithMessage("Search term cannot exceed 100 characters");
        });
    }
}
