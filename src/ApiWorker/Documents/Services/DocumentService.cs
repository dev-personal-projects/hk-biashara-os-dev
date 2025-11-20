using AutoMapper;
using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Settings;
using ApiWorker.Documents.Templates.Default;
using ApiWorker.Documents.ValueObjects;
using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Services;
using ApiWorker.Data;
using ApiWorker.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Generic service for all document operations (Invoice, Receipt, Quotation).
/// Handles: Create (voice/manual), Update, List, Render.
/// </summary>
public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;
    private readonly IBlobStorageService _blobStorage;
    private readonly IVoiceIntentService _voiceIntent;
    private readonly ICurrentUserService _currentUser;
    private readonly ITemplateService? _templateService;
    private readonly TemplateDocumentGenerator? _templateGenerator;
    private readonly DocumentSettings _settings;
    private readonly ILogger<DocumentService> _logger;
    private const string SignatureContainerName = "document-signatures";


    public DocumentService(
        ApplicationDbContext db,
        IMapper mapper,
        IBlobStorageService blobStorage,
        IVoiceIntentService voiceIntent,
        ICurrentUserService currentUser,
        ITemplateService? templateService,
        TemplateDocumentGenerator? templateGenerator,
        IOptions<DocumentSettings> settings,
        ILogger<DocumentService> logger)
    {
        _db = db;
        _mapper = mapper;
        _blobStorage = blobStorage;
        _voiceIntent = voiceIntent;
        _currentUser = currentUser;
        _templateService = templateService;
        _templateGenerator = templateGenerator;
        _settings = settings.Value;
        _logger = logger;
    }

    // ===== CREATE FROM VOICE =====
    public async Task<DocumentResponse> CreateDocumentFromVoiceAsync(CreateDocumentFromVoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate document type early
            if (!IsValidDocumentType(request.Type))
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = $"Document type must be Invoice, Receipt, or Quotation. Received: {request.Type}" 
                };

            if (string.IsNullOrWhiteSpace(request.TranscriptText) && string.IsNullOrWhiteSpace(request.AudioBlobUrl))
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = "Please provide either transcript text or audio file URL" 
                };

            // SECURITY: Verify user has access to the requested business
            var (hasAccess, accessError) = await ValidateBusinessAccessAsync(request.BusinessId, ct);
            if (!hasAccess)
                return new DocumentResponse
                {
                    Success = false,
                    Message = accessError ?? "You don't have permission to create documents for this business."
                };

            var business = await _db.Businesses.FindAsync(new object[] { request.BusinessId }, ct);
            if (business == null)
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = "Business not found. Please register your business first." 
                };

            // Extract document data from transcript using AI
            var extracted = await _voiceIntent.ExtractDocumentDataAsync(request.TranscriptText ?? string.Empty, request.Locale ?? "en-KE", ct);
            if (extracted?.Items == null || !extracted.Items.Any())
                return new DocumentResponse
                {
                    Success = false,
                    Message = $"Could not understand the {GetDocumentTypeName(request.Type).ToLower()} details from your voice input. Please speak clearly or use manual entry."
                };

            // Validate extracted items
            if (extracted.Items.Any(item => item.Quantity <= 0 || item.UnitPrice < 0))
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Invalid item quantities or prices detected. Please check your voice input and try again."
                };

            // Create document from extracted data
            TransactionalDocument document = request.Type switch
            {
                DocumentType.Invoice => new Invoice(),
                DocumentType.Receipt => new Receipt(),
                DocumentType.Quotation => new Quotation(),
                _ => throw new ArgumentException($"Unsupported document type: {request.Type}")
            };

            document.BusinessId = request.BusinessId;
            document.CreatedByUserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            document.Type = request.Type;
            document.Status = DocumentStatus.Draft;
            document.Currency = _settings.DefaultCurrency;
            document.IssuedAt = DateTimeOffset.UtcNow;
            document.CustomerName = extracted.CustomerName;
            document.CustomerPhone = extracted.CustomerPhone;
            document.Notes = extracted.Notes;
            document.Lines = extracted.Items.Select(item => new TransactionalDocumentLine
            {
                Name = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = 0
            }).ToList();

            var themeResult = await ApplyThemeAsync(document, business.Id, request.TemplateId, request.Theme, ct);
            if (!themeResult.Success)
            {
                return new DocumentResponse
                {
                    Success = false,
                    Message = themeResult.ErrorMessage ?? "Unable to use the selected template or theme."
                };
            }

            document.Number = await GenerateDocumentNumberAsync(request.BusinessId, request.Type, ct);
            CalculateTotals(document);

            // Add to appropriate DbSet
            switch (request.Type)
            {
                case DocumentType.Invoice:
                    _db.Invoices.Add((Invoice)document);
                    break;
                case DocumentType.Receipt:
                    _db.Receipts.Add((Receipt)document);
                    break;
                case DocumentType.Quotation:
                    _db.Quotations.Add((Quotation)document);
                    break;
            }

            await _db.SaveChangesAsync(ct);

            try
            {
                var (docxUrl, pdfUrl) = await RenderAndUploadAsync(document, business, ct);
                document.DocxBlobUrl = docxUrl;
                document.PdfBlobUrl = pdfUrl;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Voice document created: {DocumentNumber} ({Type})", document.Number, document.Type);

                return new DocumentResponse
                {
                    Success = true,
                    Message = $"{GetDocumentTypeName(document.Type)} created from voice successfully",
                    DocumentId = document.Id,
                    DocumentNumber = document.Number,
                    Urls = new DocumentUrls { DocxUrl = docxUrl, PdfUrl = pdfUrl, PreviewUrl = document.PreviewBlobUrl },
                    Signature = BuildSignatureDto(document)
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to render document {DocumentNumber}", document.Number);
                // Document is saved but rendering failed - return partial success
                return new DocumentResponse
                {
                    Success = false,
                    Message = $"Document saved but failed to generate files. {ex.Message}"
                };
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid document type requested: {Type}", request.Type);
            return new DocumentResponse
            {
                Success = false,
                Message = $"Invalid document type. Please use Invoice, Receipt, or Quotation."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new DocumentResponse
            {
                Success = false,
                Message = "You must be logged in to create documents. Please sign in and try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document from voice");
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to process voice input. Please check your connection and try again, or use manual entry."
            };
        }
    }

    // ===== CREATE MANUALLY =====
    public async Task<DocumentResponse> CreateDocumentManuallyAsync(CreateDocumentManuallyRequest request, CancellationToken ct = default)
    {
        try
        {
            // Validate document type early
            if (!IsValidDocumentType(request.Type))
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = $"Document type must be Invoice, Receipt, or Quotation. Received: {request.Type}" 
                };

            // SECURITY: Verify user has access to the requested business
            var (hasAccess, accessError) = await ValidateBusinessAccessAsync(request.BusinessId, ct);
            if (!hasAccess)
                return new DocumentResponse
                {
                    Success = false,
                    Message = accessError ?? "You don't have permission to create documents for this business."
                };

            var business = await _db.Businesses.FindAsync(new object[] { request.BusinessId }, ct);
            if (business == null)
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = "Business not found. Please register your business first." 
                };

            // Validate lines are not empty
            if (request.Lines == null || !request.Lines.Any())
                return new DocumentResponse
                {
                    Success = false,
                    Message = "At least one line item is required. Please add products or services to your document."
                };

            // Validate currency
            if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Currency must be a 3-letter code (e.g., KES, USD, EUR)"
                };

            // Create document based on type
            TransactionalDocument document = request.Type switch
            {
                DocumentType.Invoice => _mapper.Map<Invoice>(request),
                DocumentType.Receipt => _mapper.Map<Receipt>(request),
                DocumentType.Quotation => _mapper.Map<Quotation>(request),
                _ => throw new ArgumentException($"Unsupported document type: {request.Type}")
            };

            document.BusinessId = request.BusinessId;
            document.CreatedByUserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
            document.Type = request.Type;
            var themeResult = await ApplyThemeAsync(document, business.Id, request.TemplateId, request.Theme, ct);
            if (!themeResult.Success)
            {
                return new DocumentResponse
                {
                    Success = false,
                    Message = themeResult.ErrorMessage ?? "Unable to use the selected template or theme."
                };
            }
            document.Number = await GenerateDocumentNumberAsync(request.BusinessId, request.Type, ct);
            CalculateTotals(document);

            // Add to appropriate DbSet
            switch (request.Type)
            {
                case DocumentType.Invoice:
                    _db.Invoices.Add((Invoice)document);
                    break;
                case DocumentType.Receipt:
                    _db.Receipts.Add((Receipt)document);
                    break;
                case DocumentType.Quotation:
                    _db.Quotations.Add((Quotation)document);
                    break;
            }

            await _db.SaveChangesAsync(ct);

            try
            {
                var (docxUrl, pdfUrl) = await RenderAndUploadAsync(document, business, ct);
                document.DocxBlobUrl = docxUrl;
                document.PdfBlobUrl = pdfUrl;
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation("Document created: {DocumentNumber} ({Type}) for business {BusinessId}", document.Number, document.Type, business.Id);

                return new DocumentResponse
                {
                    Success = true,
                    Message = $"{GetDocumentTypeName(document.Type)} created successfully",
                    DocumentId = document.Id,
                    DocumentNumber = document.Number,
                    Urls = new DocumentUrls
                    {
                        DocxUrl = docxUrl,
                        PdfUrl = pdfUrl,
                        PreviewUrl = document.PreviewBlobUrl
                    },
                    Signature = BuildSignatureDto(document)
                };
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Failed to render document {DocumentNumber}", document.Number);
                // Document is saved but rendering failed - return partial success
                return new DocumentResponse
                {
                    Success = false,
                    Message = $"Document saved but failed to generate files. {ex.Message}"
                };
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid document type requested: {Type}", request.Type);
            return new DocumentResponse
            {
                Success = false,
                Message = $"Invalid document type. Please use Invoice, Receipt, or Quotation."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new DocumentResponse
            {
                Success = false,
                Message = "You must be logged in to create documents. Please sign in and try again."
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating document");
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to save document. Please check your input and try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document manually");
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to create document. Please check your input and try again."
            };
        }
    }

    // ===== GET DOCUMENT =====
    public async Task<DocumentDetailResponse> GetDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        try
        {
            if (documentId == Guid.Empty)
                return new DocumentDetailResponse
                {
                    Success = false,
                    Message = "Invalid document ID. Please provide a valid document ID."
                };

            // SECURITY: Filter by business to prevent unauthorized access
            if (!_currentUser.BusinessId.HasValue)
                return new DocumentDetailResponse
                {
                    Success = false,
                    Message = "No active business context. Please select a business first."
                };

            var document = await _db.TransactionalDocuments
                .Include(d => d.Lines)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.BusinessId == _currentUser.BusinessId.Value, ct);

            if (document == null)
                return new DocumentDetailResponse
                {
                    Success = false,
                    Message = "Document not found. The document may have been deleted or the ID is incorrect."
                };

            // Check if user has access to this document
            if (!_currentUser.UserId.HasValue || document.CreatedByUserId != _currentUser.UserId.Value)
                return new DocumentDetailResponse
                {
                    Success = false,
                    Message = "You don't have permission to view this document."
                };

            var documentDto = _mapper.Map<DocumentDto>(document);

            return new DocumentDetailResponse
            {
                Success = true,
                Message = $"{GetDocumentTypeName(document.Type)} retrieved successfully",
                Document = documentDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
            return new DocumentDetailResponse
            {
                Success = false,
                Message = "Unable to retrieve document. Please check your connection and try again."
            };
        }
    }

    // ===== LIST DOCUMENTS =====
    public async Task<ListDocumentsResponse> ListDocumentsAsync(ListDocumentsRequest request, CancellationToken ct = default)
    {
        try
        {
            // SECURITY: Filter by current user's business to prevent data leakage
            if (!_currentUser.BusinessId.HasValue)
                return new ListDocumentsResponse
                {
                    Success = false,
                    Message = "No active business context. Please select a business first.",
                    Documents = new List<DocumentSummary>(),
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                };

            var query = _db.TransactionalDocuments
                .Where(d => d.BusinessId == _currentUser.BusinessId.Value);

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

            var totalCount = await query.CountAsync(ct);

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

    // ===== UPDATE DOCUMENT =====
    public async Task<DocumentResponse> UpdateDocumentAsync(UpdateDocumentRequest request, CancellationToken ct = default)
    {
        try
        {
            if (request.DocumentId == Guid.Empty)
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = "Invalid document ID. Please provide a valid document ID." 
                };

            // SECURITY: Filter by business to prevent unauthorized access
            if (!_currentUser.BusinessId.HasValue)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "No active business context. Please select a business first."
                };

            var document = await _db.TransactionalDocuments
                .Include(d => d.Lines)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.BusinessId == _currentUser.BusinessId.Value, ct);

            if (document == null)
                return new DocumentResponse 
                { 
                    Success = false, 
                    Message = "Document not found. The document may have been deleted or the ID is incorrect." 
                };

            // Check if user has access to this document
            if (!_currentUser.UserId.HasValue || document.CreatedByUserId != _currentUser.UserId.Value)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "You don't have permission to edit this document."
                };

            if (document.Status != DocumentStatus.Draft)
                return new DocumentResponse
                {
                    Success = false,
                    Message = $"This {GetDocumentTypeName(document.Type).ToLower()} cannot be edited because it's already finalized. Only draft documents can be edited."
                };

            // Update fields
            if (request.Customer != null)
            {
                document.CustomerName = request.Customer.Name;
                document.CustomerPhone = request.Customer.Phone;
                document.CustomerEmail = request.Customer.Email;
            }

            if (request.Lines != null && request.Lines.Any())
            {
                // Validate lines before updating
                if (request.Lines.Any(line => line.Quantity <= 0))
                    return new DocumentResponse
                    {
                        Success = false,
                        Message = "All line items must have a quantity greater than 0."
                    };

                if (request.Lines.Any(line => line.UnitPrice < 0))
                    return new DocumentResponse
                    {
                        Success = false,
                        Message = "Item prices cannot be negative."
                    };

                _db.TransactionalDocumentLines.RemoveRange(document.Lines);
                document.Lines = _mapper.Map<List<TransactionalDocumentLine>>(request.Lines);
                CalculateTotals(document);
            }

            if (request.DueAt.HasValue)
                document.DueAt = request.DueAt;

            if (!string.IsNullOrWhiteSpace(request.Notes))
                document.Notes = request.Notes;

            if (!string.IsNullOrWhiteSpace(request.Reference))
                document.Reference = request.Reference;

            document.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Document updated: {DocumentNumber}", document.Number);

            return new DocumentResponse
            {
                Success = true,
                Message = $"{GetDocumentTypeName(document.Type)} updated successfully",
                DocumentId = document.Id,
                DocumentNumber = document.Number,
                Signature = BuildSignatureDto(document)
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating document {DocumentId}", request.DocumentId);
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to save changes. Please check your input and try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update document {DocumentId}", request.DocumentId);
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to update document. Please check your connection and try again."
            };
        }
    }

    // ===== SIGN DOCUMENT =====
    public async Task<DocumentResponse> SignDocumentAsync(SignDocumentRequest request, CancellationToken ct = default)
    {
        TransactionalDocument? document = null;
        try
        {
            if (request.DocumentId == Guid.Empty)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Invalid document ID. Please provide a valid document ID."
                };

            if (string.IsNullOrWhiteSpace(request.SignatureBase64))
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Signature image is required."
                };

            // SECURITY: Filter by business to prevent unauthorized access
            if (!_currentUser.BusinessId.HasValue)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "No active business context. Please select a business first."
                };

            document = await _db.TransactionalDocuments
                .Include(d => d.Lines)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.BusinessId == _currentUser.BusinessId.Value, ct);

            if (document == null)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Document not found. The document may have been deleted or the ID is incorrect."
                };

            // Allow any business member to sign (not just the creator)
            // This enables business owners and authorized staff to sign documents
            if (!_currentUser.UserId.HasValue)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "You must be logged in to sign documents."
                };

            // Verify user is a member of the document's business
            var isBusinessMember = await _db.Memberships
                .AnyAsync(m => m.UserId == _currentUser.UserId.Value 
                    && m.BusinessId == document.BusinessId 
                    && m.Status == ApiWorker.Authentication.Enum.MembershipStatus.Active, ct);

            if (!isBusinessMember)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "You don't have permission to sign this document. You must be a member of the business that owns this document."
                };

            var business = await _db.Businesses.FirstOrDefaultAsync(b => b.Id == document.BusinessId, ct);
            if (business == null)
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Business not found. Please register your business first."
                };

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(request.SignatureBase64);
            }
            catch (FormatException)
            {
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Signature must be a valid base64 string."
                };
            }

            if (signatureBytes.Length == 0)
            {
                return new DocumentResponse
                {
                    Success = false,
                    Message = "Signature cannot be empty."
                };
            }

            await using var signatureStream = new MemoryStream(signatureBytes);
            var signatureFileName = $"{document.Number}-signature.png";
            var signatureUrl = await _blobStorage.UploadAsync(signatureStream, signatureFileName, SignatureContainerName, "image/png", ct);

            document.SignatureBlobUrl = signatureUrl;
            document.SignedBy = request.SignerName;
            document.SignedAt = DateTimeOffset.UtcNow;
            document.SignatureNotes = request.Notes;
            document.Status = DocumentStatus.Signed;
            document.UpdatedAt = DateTimeOffset.UtcNow;

            var (docxUrl, pdfUrl) = await RenderAndUploadAsync(document, business, ct);
            document.DocxBlobUrl = docxUrl;
            document.PdfBlobUrl = pdfUrl;

            await _db.SaveChangesAsync(ct);

            return new DocumentResponse
            {
                Success = true,
                Message = $"{GetDocumentTypeName(document.Type)} signed successfully",
                DocumentId = document.Id,
                DocumentNumber = document.Number,
                Urls = new DocumentUrls
                {
                    DocxUrl = docxUrl,
                    PdfUrl = pdfUrl,
                    PreviewUrl = document.PreviewBlobUrl
                },
                Signature = BuildSignatureDto(document)
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to render signed document {DocumentId}. Inner exception: {InnerException}", 
                request.DocumentId, ex.InnerException?.Message ?? ex.Message);
            return new DocumentResponse
            {
                Success = false,
                Message = $"Signature saved but failed to regenerate files. {ex.Message}",
                DocumentId = request.DocumentId,
                DocumentNumber = document?.Number
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign document {DocumentId}", request.DocumentId);
            return new DocumentResponse
            {
                Success = false,
                Message = "Unable to sign document. Please try again."
            };
        }
    }

    // ===== HELPER METHODS =====

    private async Task<string> GenerateDocumentNumberAsync(Guid businessId, DocumentType type, CancellationToken ct)
    {
        var prefix = type switch
        {
            DocumentType.Invoice => _settings.Numbering.InvoicePrefix,
            DocumentType.Receipt => _settings.Numbering.ReceiptPrefix,
            DocumentType.Quotation => _settings.Numbering.QuotationPrefix,
            _ => "DOC-"
        };

        // Get last document of this type for this business
        var lastDocument = await _db.TransactionalDocuments
            .Where(d => d.BusinessId == businessId && d.Type == type)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var sequence = 1;
        if (lastDocument != null)
        {
            var parts = lastDocument.Number.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[^1], out var lastSeq))
                sequence = lastSeq + 1;
        }

        var yearMonth = DateTimeOffset.UtcNow.ToString("yyyyMM");
        return $"{prefix}{yearMonth}-{sequence:D4}";
    }

    private void CalculateTotals(TransactionalDocument document)
    {
        document.Subtotal = 0;
        document.Tax = 0;

        foreach (var line in document.Lines)
        {
            line.LineTotal = line.Quantity * line.UnitPrice;
            document.Subtotal += line.LineTotal;
            document.Tax += line.LineTotal * line.TaxRate;
        }

        document.Total = document.Subtotal + document.Tax;
    }

    private async Task<(string docxUrl, string pdfUrl)> RenderAndUploadAsync(TransactionalDocument document, Business business, CancellationToken ct)
    {
        try
        {
            // Validate document has required data for rendering
            if (document.Lines == null || !document.Lines.Any())
            {
                throw new InvalidOperationException("Document must have at least one line item to render.");
            }

            var theme = DocumentTheme.FromJson(document.AppliedThemeJson);
            var signature = await BuildSignatureRenderAsync(document, ct);

            MemoryStream docxStream;

            // Use template if TemplateId is provided and services are available
            if (document.TemplateId.HasValue && _templateService != null && _templateGenerator != null)
            {
                var template = await _templateService.GetTemplateAsync(document.TemplateId.Value, ct);
                if (template != null && !string.IsNullOrWhiteSpace(template.BlobPath))
                {
                    // Merge template
                    docxStream = await _templateGenerator.MergeTemplateAsync(
                        template.BlobPath,
                        document,
                        business,
                        theme,
                        signature,
                        ct);
                }
                else
                {
                    // Fallback to programmatic generation
                    _logger.LogWarning("Template {TemplateId} not found, using programmatic generation", document.TemplateId);
                    docxStream = OpenXmlDocumentGenerator.GenerateDocument(document, business, theme, signature);
                }
            }
            else
            {
                // Use programmatic generation
                docxStream = OpenXmlDocumentGenerator.GenerateDocument(document, business, theme, signature);
            }

            docxStream.Position = 0;
            var docxFileName = $"{document.Number}.docx";
            var containerName = GetContainerName(document.Type);
            var docxUrl = await _blobStorage.UploadAsync(docxStream, docxFileName, containerName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ct);

            // Always use QuestPDF for PDF generation (can be enhanced to convert DOCX to PDF if needed)
            var pdfBytes = QuestPdfDocumentGenerator.GenerateDocumentPdf(document, business, theme, signature);
            using var pdfStream = new MemoryStream(pdfBytes);
            pdfStream.Position = 0;
            var pdfFileName = $"{document.Number}.pdf";
            var pdfUrl = await _blobStorage.UploadAsync(pdfStream, pdfFileName, containerName, "application/pdf", ct);

            var previewBytes = QuestPdfDocumentGenerator.GenerateDocumentPreview(document, business, theme, signature);
            using var previewStream = new MemoryStream(previewBytes);
            previewStream.Position = 0;
            var previewFileName = $"{document.Number}.png";
            document.PreviewBlobUrl = await _blobStorage.UploadAsync(previewStream, previewFileName, "doc-previews", "image/png", ct);

            return (docxUrl, pdfUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render or upload document {DocumentNumber}", document.Number);
            throw new InvalidOperationException($"Failed to generate {GetDocumentTypeName(document.Type).ToLower()} files. Please try again.", ex);
        }
    }

    private static string GetContainerName(DocumentType type)
    {
        return type switch
        {
            DocumentType.Invoice => "invoices",
            DocumentType.Receipt => "receipts",
            DocumentType.Quotation => "quotations",
            _ => "documents"
        };
    }

    private static string GetDocumentTypeName(DocumentType type)
    {
        return type switch
        {
            DocumentType.Invoice => "Invoice",
            DocumentType.Receipt => "Receipt",
            DocumentType.Quotation => "Quotation",
            _ => "Document"
        };
    }

    private static bool IsValidDocumentType(DocumentType type)
    {
        return type == DocumentType.Invoice || 
               type == DocumentType.Receipt || 
               type == DocumentType.Quotation;
    }

    private async Task<(bool HasAccess, string? ErrorMessage)> ValidateBusinessAccessAsync(Guid businessId, CancellationToken ct)
    {
        if (!_currentUser.UserId.HasValue)
            return (false, "You must be logged in to perform this action.");

        var hasAccess = await _db.Memberships
            .AnyAsync(m => m.UserId == _currentUser.UserId.Value 
                && m.BusinessId == businessId 
                && m.Status == ApiWorker.Authentication.Enum.MembershipStatus.Active, ct);

        return hasAccess 
            ? (true, null) 
            : (false, "You don't have permission to access this business.");
    }

    private async Task<(bool Success, string? ErrorMessage)> ApplyThemeAsync(TransactionalDocument document, Guid businessId, Guid? templateId, DocumentThemeDto? inlineTheme, CancellationToken ct)
    {
        if (inlineTheme != null)
        {
            if (templateId.HasValue)
            {
                var template = await _db.DocumentTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && (t.BusinessId == null || t.BusinessId == businessId), ct);

                if (template == null)
                    return (false, "Template not found or you do not have permission to use it.");

                document.TemplateId = template.Id;
            }
            else
            {
                document.TemplateId = null;
            }

            var theme = DocumentTheme.FromDto(inlineTheme);
            document.AppliedThemeJson = theme.ToJson();
            return (true, null);
        }

        if (templateId.HasValue)
        {
            var template = await _db.DocumentTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && (t.BusinessId == null || t.BusinessId == businessId), ct);

            if (template == null)
                return (false, "Template not found or you do not have permission to use it.");

            var theme = DocumentTheme.FromJson(template.ThemeJson);
            document.TemplateId = template.Id;
            document.AppliedThemeJson = theme.ToJson();
            return (true, null);
        }

        document.AppliedThemeJson = DocumentTheme.Default.ToJson();
        document.TemplateId = null;
        return (true, null);
    }

    private async Task<DocumentSignatureRender> BuildSignatureRenderAsync(TransactionalDocument document, CancellationToken ct)
    {
        var bytes = await DownloadSignatureAsync(document.SignatureBlobUrl, ct);
        return new DocumentSignatureRender
        {
            ImageBytes = bytes,
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            Notes = document.SignatureNotes
        };
    }

    private async Task<byte[]?> DownloadSignatureAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch
        {
            return null;
        }
    }

    private DocumentSignatureDto? BuildSignatureDto(TransactionalDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.SignatureBlobUrl) && string.IsNullOrWhiteSpace(document.SignedBy))
            return null;

        return new DocumentSignatureDto
        {
            IsSigned = !string.IsNullOrWhiteSpace(document.SignatureBlobUrl),
            SignedBy = document.SignedBy,
            SignedAt = document.SignedAt,
            SignatureUrl = document.SignatureBlobUrl,
            Notes = document.SignatureNotes
        };
    }
}
