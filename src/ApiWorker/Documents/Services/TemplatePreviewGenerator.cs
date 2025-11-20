using ApiWorker.Documents.Entities;
using ApiWorker.Documents.ValueObjects;
using ApiWorker.Documents.Templates.Default;
using ApiWorker.Authentication.Entities;
using QuestPDF.Infrastructure;
using DocumentTemplate = ApiWorker.Documents.Entities.Template;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Generates preview images for templates using sample data.
/// </summary>
public sealed class TemplatePreviewGenerator
{
    private readonly ILogger<TemplatePreviewGenerator> _logger;

    public TemplatePreviewGenerator(ILogger<TemplatePreviewGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a PNG preview image for a template using sample document data.
    /// </summary>
    public async Task<byte[]> GeneratePreviewAsync(DocumentTemplate template, CancellationToken ct = default)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Create sample business
            var sampleBusiness = new Business
            {
                Name = "Sample Business",
                Phone = "+254712345678",
                Email = "info@samplebusiness.com",
                County = "Nairobi",
                Town = "Westlands"
            };

            // Create sample document based on template type
            var sampleDocument = CreateSampleDocument(template.Type);

            // Get theme from template or use default
            var theme = string.IsNullOrWhiteSpace(template.ThemeJson)
                ? DocumentTheme.Default
                : DocumentTheme.FromJson(template.ThemeJson);

            // Create empty signature
            var signature = new DocumentSignatureRender
            {
                ImageBytes = null,
                SignedBy = null,
                SignedAt = null,
                Notes = null
            };

            // Generate preview image
            var previewBytes = await Task.Run(() => QuestPdfDocumentGenerator.GenerateDocumentPreview(
                sampleDocument,
                sampleBusiness,
                theme,
                signature), ct);

            return previewBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for template {TemplateId}", template.Id);
            throw new InvalidOperationException($"Failed to generate template preview: {ex.Message}", ex);
        }
    }

    private TransactionalDocument CreateSampleDocument(DocumentType type)
    {
        TransactionalDocument document = type switch
        {
            DocumentType.Invoice => new Invoice(),
            DocumentType.Receipt => new Receipt(),
            DocumentType.Quotation => new Quotation(),
            _ => throw new ArgumentException($"Unsupported document type: {type}")
        };

        document.Id = Guid.NewGuid();
        document.Number = type switch
        {
            DocumentType.Invoice => "INV-202501-0001",
            DocumentType.Receipt => "RCPT-202501-0001",
            DocumentType.Quotation => "QUO-202501-0001",
            _ => "DOC-202501-0001"
        };
        document.Type = type;
        document.Status = DocumentStatus.Draft;
        document.Currency = "KES";
        document.CustomerName = "John Doe";
        document.CustomerPhone = "+254712345679";
        document.CustomerEmail = "john@example.com";
        document.BillingAddressLine1 = "123 Main Street";
        document.BillingCity = "Nairobi";
        document.BillingCountry = "Kenya";
        document.IssuedAt = DateTimeOffset.UtcNow;
        document.DueAt = DateTimeOffset.UtcNow.AddDays(30);
        document.Notes = "This is a sample document for preview purposes.";
        document.Reference = "REF-001";

        // Add sample line items
        document.Lines = new List<TransactionalDocumentLine>
        {
            new TransactionalDocumentLine
            {
                Id = Guid.NewGuid(),
                Name = "Sample Product 1",
                Description = "Description for product 1",
                Quantity = 2,
                UnitPrice = 1000.00m,
                TaxRate = 0.16m,
                LineTotal = 2000.00m
            },
            new TransactionalDocumentLine
            {
                Id = Guid.NewGuid(),
                Name = "Sample Product 2",
                Description = "Description for product 2",
                Quantity = 3,
                UnitPrice = 500.00m,
                TaxRate = 0.16m,
                LineTotal = 1500.00m
            }
        };

        // Calculate totals
        document.Subtotal = document.Lines.Sum(l => l.LineTotal);
        document.Tax = document.Lines.Sum(l => l.LineTotal * l.TaxRate);
        document.Total = document.Subtotal + document.Tax;

        return document;
    }
}

