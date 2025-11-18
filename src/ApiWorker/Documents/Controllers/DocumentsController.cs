using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWorker.Documents.Controllers;

/// <summary>
/// API controller for managing business documents (Invoice, Receipt, Quotation).
/// Supports manual creation, voice creation, updates, and listing.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IValidator<CreateDocumentManuallyRequest> _manualValidator;
    private readonly IValidator<CreateDocumentFromVoiceRequest> _voiceValidator;
    private readonly IValidator<UpdateDocumentRequest> _updateValidator;

    public DocumentsController(
        IDocumentService documentService,
        IValidator<CreateDocumentManuallyRequest> manualValidator,
        IValidator<CreateDocumentFromVoiceRequest> voiceValidator,
        IValidator<UpdateDocumentRequest> updateValidator)
    {
        _documentService = documentService;
        _manualValidator = manualValidator;
        _voiceValidator = voiceValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Create document manually (Invoice, Receipt, or Quotation)</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentResponse>> CreateManually([FromBody] CreateDocumentManuallyRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new DocumentResponse { Success = false, Message = "Request body is required" });

        var validation = await _manualValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errorMessages = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = errorMessages.Count == 1 
                    ? errorMessages.First() 
                    : $"Please fix the following errors: {string.Join("; ", errorMessages)}"
            });
        }

        var result = await _documentService.CreateDocumentManuallyAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Create document from voice input (transcript or audio URL)</summary>
    [HttpPost("voice")]
    public async Task<ActionResult<DocumentResponse>> CreateFromVoice([FromBody] CreateDocumentFromVoiceRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new DocumentResponse { Success = false, Message = "Request body is required" });

        var validation = await _voiceValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errorMessages = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = errorMessages.Count == 1 
                    ? errorMessages.First() 
                    : $"Please fix the following errors: {string.Join("; ", errorMessages)}"
            });
        }

        var result = await _documentService.CreateDocumentFromVoiceAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get document details by ID</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDetailResponse>> GetDocument(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
            return BadRequest(new DocumentDetailResponse 
            { 
                Success = false, 
                Message = "Invalid document ID. Please provide a valid document ID." 
            });

        var result = await _documentService.GetDocumentAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update document (only allowed for Draft status)</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentResponse>> UpdateDocument(Guid id, [FromBody] UpdateDocumentRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new DocumentResponse { Success = false, Message = "Request body is required" });

        if (id != request.DocumentId)
            return BadRequest(new DocumentResponse 
            { 
                Success = false, 
                Message = "Document ID in URL does not match the ID in request body" 
            });

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errorMessages = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = errorMessages.Count == 1 
                    ? errorMessages.First() 
                    : $"Please fix the following errors: {string.Join("; ", errorMessages)}"
            });
        }

        var result = await _documentService.UpdateDocumentAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>List documents with optional filters (type, status, date range, search)</summary>
    [HttpGet]
    public async Task<ActionResult<ListDocumentsResponse>> ListDocuments([FromQuery] ListDocumentsRequest request, CancellationToken ct)
    {
        var result = await _documentService.ListDocumentsAsync(request, ct);
        return Ok(result);
    }
}