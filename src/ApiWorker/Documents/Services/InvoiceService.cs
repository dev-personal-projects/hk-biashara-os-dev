using AutoMapper;
using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;
using ApiWorker.Documents.Enums;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Settings;
using ApiWorker.Documents.Templates.Default;
using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Services;
using ApiWorker.Data;
using ApiWorker.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Main service orchestrating all invoice operations.
/// Handles: Create (voice/manual), Update, List, Render, Share.
/// </summary>
public sealed class InvoiceService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IBlobStorageService _blobStorage;
    private readonly IVoiceIntentService _voiceIntent;
    private readonly ICurrentUserService _currentUser;
    private readonly DocumentSettings _settings;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext db,
        IMapper mapper,
        IBlobStorageService blobStorage,
        IVoiceIntentService voiceIntent,
        ICurrentUserService currentUser,
        IOptions<DocumentSettings> settings,
        ILogger<InvoiceService> logger)
    {
        _db = db;
        _mapper = mapper;
        _blobStorage = blobStorage;
        _voiceIntent = voiceIntent;
        _currentUser = currentUser;
        _settings = settings.Value;
        _logger = logger;
    }

    // ===== CREATE FROM VOICE =====
    public async Task<DocumentResponse> CreateInvoiceFromVoiceAsync(CreateInvoiceFromVoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TranscriptText))
                return new DocumentResponse { Success = false, Message = "Transcript text is required" };

            var business = await _db.Businesses.FindAsync(new object[] { request.BusinessId }, ct);
            if (business == null)
                return new DocumentResponse { Success = false, Message = "Business not found" };

            // Extract invoice data from transcript using AI
            var extracted = await _voiceIntent.ExtractInvoiceDataAsync(request.TranscriptText, request.Locale ?? "en-KE", ct);
            if (extracted?.Items == null || !extracted.Items.Any())
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Could not understand the invoice details. Please try again or use manual entry."
                };

            // Create invoice from extracted data
            var invoice = new Invoice
            {
                BusinessId = request.BusinessId,
                CreatedByUserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated"),
                Type = DocumentType.Invoice,
                Status = DocumentStatus.Draft,
                Currency = _settings.DefaultCurrency,
                IssuedAt = DateTimeOffset.UtcNow,
                CustomerName = extracted.CustomerName,
                CustomerPhone = extracted.CustomerPhone,
                Notes = extracted.Notes,
                Lines = extracted.Items.Select(item => new InvoiceLine
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = 0
                }).ToList()
            };

            invoice.Number = await GenerateInvoiceNumberAsync(request.BusinessId, ct);
            CalculateTotals(invoice);

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            var (docxUrl, pdfUrl) = await RenderAndUploadAsync(invoice, business, ct);
            invoice.DocxBlobUrl = docxUrl;
            invoice.PdfBlobUrl = pdfUrl;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Voice invoice created: {InvoiceNumber}", invoice.Number);

            return new DocumentResponse
            {
                Success = true,
                Message = "Invoice created from voice successfully",
                DocumentId = invoice.Id,
                DocumentNumber = invoice.Number,
                Urls = new DocumentUrls { DocxUrl = docxUrl, PdfUrl = pdfUrl, PreviewUrl = invoice.PreviewBlobUrl }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice from voice");
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to process voice input. Please try again or use manual entry."
            };
        }
    }

    // ===== CREATE MANUALLY =====
    public async Task<DocumentResponse> CreateInvoiceManuallyAsync(CreateInvoiceManuallyRequest request, CancellationToken ct = default)
    {
        try
        {
            // Get business
            var business = await _db.Businesses.FindAsync(new object[] { request.BusinessId }, ct);
            if (business == null)
                return new DocumentResponse { Success = false, Message = "Business not found" };

            // Map DTO to entity
            var invoice = _mapper.Map<Invoice>(request);
            invoice.BusinessId = request.BusinessId;
            invoice.CreatedByUserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            
            // Generate invoice number
            invoice.Number = await GenerateInvoiceNumberAsync(request.BusinessId, ct);
            
            // Calculate totals
            CalculateTotals(invoice);

            // Save to database
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            // Render documents (DOCX and PDF)
            var (docxUrl, pdfUrl) = await RenderAndUploadAsync(invoice, business, ct);
            
            // Update invoice with URLs
            invoice.DocxBlobUrl = docxUrl;
            invoice.PdfBlobUrl = pdfUrl;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Invoice created: {InvoiceNumber} for business {BusinessId}", invoice.Number, business.Id);

            return new DocumentResponse
            {
                Success = true,
                Message = "Invoice created successfully",
                DocumentId = invoice.Id,
                DocumentNumber = invoice.Number,
                Urls = new DocumentUrls
                {
                    DocxUrl = docxUrl,
                    PdfUrl = pdfUrl,
                    PreviewUrl = invoice.PreviewBlobUrl
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice manually");
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to create invoice. Please check your input and try again."
            };
        }
    }

    // ===== GET INVOICE =====
    public async Task<InvoiceDetailResponse> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        try
        {
            var invoice = await _db.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null)
                return new InvoiceDetailResponse
                {
                    Success = false,
                    Message = "Invoice not found"
                };

            var invoiceDto = _mapper.Map<InvoiceDto>(invoice);

            return new InvoiceDetailResponse
            {
                Success = true,
                Message = "Invoice retrieved successfully",
                Invoice = invoiceDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get invoice {InvoiceId}", invoiceId);
            return new InvoiceDetailResponse
            {
                Success = false,
                Message = "Unable to retrieve invoice. Please try again."
            };
        }
    }

    // ===== LIST DOCUMENTS =====
    public async Task<ListDocumentsResponse> ListDocumentsAsync(ListDocumentsRequest request, CancellationToken ct = default)
    {
        try
        {
            var query = _db.Invoices.AsQueryable();

            // Apply filters
            if (request.Type.HasValue)
                query = query.Where(d => d.Type == request.Type.Value);

            if (request.Status.HasValue)
                query = query.Where(d => d.Status == request.Status.Value);

            if (request.FromDate.HasValue)
                query = query.Where(d => d.IssuedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(d => d.IssuedAt <= request.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLower();
                query = query.Where(d => 
                    d.Number.ToLower().Contains(searchTerm) ||
                    (d.CustomerName != null && d.CustomerName.ToLower().Contains(searchTerm)));
            }

            // Get total count
            var totalCount = await query.CountAsync(ct);

            // Apply pagination
            var documents = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var summaries = _mapper.Map<List<DocumentSummary>>(documents);

            return new ListDocumentsResponse
            {
                Success = true,
                Message = $"Found {totalCount} documents",
                Documents = summaries,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents");
            return new ListDocumentsResponse
            {
                Success = false,
                Message = "Unable to retrieve documents. Please try again."
            };
        }
    }

    // ===== UPDATE INVOICE =====
    public async Task<DocumentResponse> UpdateInvoiceAsync(UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            var invoice = await _db.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

            if (invoice == null)
                return new DocumentResponse { Success = false, Message = "Invoice not found" };

            // Only allow updates to Draft invoices
            if (invoice.Status != DocumentStatus.Draft)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Only draft invoices can be edited"
                };

            // Update fields
            if (request.Customer != null)
            {
                invoice.CustomerName = request.Customer.Name;
                invoice.CustomerPhone = request.Customer.Phone;
                invoice.CustomerEmail = request.Customer.Email;
            }

            if (request.Lines != null && request.Lines.Any())
            {
                // Remove old lines
                _db.InvoiceLines.RemoveRange(invoice.Lines);
                
                // Add new lines
                invoice.Lines = _mapper.Map<List<InvoiceLine>>(request.Lines);
                
                // Recalculate totals
                CalculateTotals(invoice);
            }

            if (request.DueAt.HasValue)
                invoice.DueAt = request.DueAt;

            if (!string.IsNullOrWhiteSpace(request.Notes))
                invoice.Notes = request.Notes;

            if (!string.IsNullOrWhiteSpace(request.Reference))
                invoice.Reference = request.Reference;

            invoice.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Invoice updated: {InvoiceNumber}", invoice.Number);

            return new DocumentResponse
            {
                Success = true,
                Message = "Invoice updated successfully",
                DocumentId = invoice.Id,
                DocumentNumber = invoice.Number
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update invoice {InvoiceId}", request.InvoiceId);
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to update invoice. Please try again."
            };
        }
    }

    // ===== SHARE DOCUMENT =====
    public async Task<ShareDocumentResponse> ShareDocumentAsync(ShareDocumentRequest request, CancellationToken ct = default)
    {
        try
        {
            var invoice = await _db.Invoices.FindAsync(new object[] { request.DocumentId }, ct);
            if (invoice == null)
                return new ShareDocumentResponse { Success = false, Message = "Document not found" };

            if (string.IsNullOrEmpty(invoice.PdfBlobUrl))
                return new ShareDocumentResponse { Success = false, Message = "Document not yet rendered" };

            // Create share log
            var shareLog = new ShareLog
            {
                DocumentId = request.DocumentId,
                Channel = Enum.Parse<ShareChannel>(request.Channel),
                Target = request.Target ?? string.Empty,
                Success = true,
                SentAt = DateTimeOffset.UtcNow
            };

            _db.ShareLogs.Add(shareLog);
            await _db.SaveChangesAsync(ct);

            // TODO: Implement actual sharing (WhatsApp, Email)
            // For now, just return the PDF URL

            return new ShareDocumentResponse
            {
                Success = true,
                Message = $"Document shared via {request.Channel}",
                ShareLogId = shareLog.Id,
                PublicUrl = invoice.PdfBlobUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share document {DocumentId}", request.DocumentId);
            return new ShareDocumentResponse
            {
                Success = false,
                Message = "Unable to share document. Please try again."
            };
        }
    }

    // ===== HELPER METHODS =====

    private async Task<string> GenerateInvoiceNumberAsync(Guid businessId, CancellationToken ct)
    {
        var prefix = _settings.Numbering.InvoicePrefix;
        var pattern = _settings.Numbering.Pattern;
        
        // Get next sequence number for this business
        var lastInvoice = await _db.Invoices
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var sequence = 1;
        if (lastInvoice != null)
        {
            // Extract sequence from last number (e.g., "INV-202401-0005" -> 5)
            var parts = lastInvoice.Number.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[^1], out var lastSeq))
                sequence = lastSeq + 1;
        }

        // Format: INV-202401-0001
        var yearMonth = DateTimeOffset.UtcNow.ToString("yyyyMM");
        return $"{prefix}{yearMonth}-{sequence:D4}";
    }

    private void CalculateTotals(Invoice invoice)
    {
        invoice.Subtotal = 0;
        invoice.Tax = 0;

        foreach (var line in invoice.Lines)
        {
            line.LineTotal = line.Quantity * line.UnitPrice;
            invoice.Subtotal += line.LineTotal;
            invoice.Tax += line.LineTotal * line.TaxRate;
        }

        invoice.Total = invoice.Subtotal + invoice.Tax;
    }

    private async Task<(string docxUrl, string pdfUrl)> RenderAndUploadAsync(Invoice invoice, Authentication.Entities.Business business, CancellationToken ct)
    {
        // Generate DOCX
        using var docxStream = OpenXmlInvoiceGenerator.GenerateInvoice(invoice, business);
        docxStream.Position = 0;
        var docxFileName = $"{invoice.Number}.docx";
        var docxUrl = await _blobStorage.UploadAsync(docxStream, docxFileName, "invoices", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

        // Generate PDF
        var pdfBytes = QuestPdfInvoiceGenerator.GenerateInvoicePdf(invoice, business);
        using var pdfStream = new MemoryStream(pdfBytes);
        pdfStream.Position = 0;
        var pdfFileName = $"{invoice.Number}.pdf";
        var pdfUrl = await _blobStorage.UploadAsync(pdfStream, pdfFileName, "invoices", "application/pdf", ct);

        // Generate PNG preview
        var previewBytes = QuestPdfInvoiceGenerator.GenerateInvoicePreview(invoice, business);
        using var previewStream = new MemoryStream(previewBytes);
        previewStream.Position = 0;
        var previewFileName = $"{invoice.Number}.png";
        invoice.PreviewBlobUrl = await _blobStorage.UploadAsync(previewStream, previewFileName, "doc-previews", "image/png", ct);

        return (docxUrl, pdfUrl);
    }
}
