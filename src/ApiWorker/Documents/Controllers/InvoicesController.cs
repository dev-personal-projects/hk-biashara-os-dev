using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWorker.Documents.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IValidator<CreateInvoiceManuallyRequest> _manualValidator;
    private readonly IValidator<CreateInvoiceFromVoiceRequest> _voiceValidator;
    private readonly IValidator<UpdateInvoiceRequest> _updateValidator;

    public InvoicesController(
        IDocumentService documentService,
        IValidator<CreateInvoiceManuallyRequest> manualValidator,
        IValidator<CreateInvoiceFromVoiceRequest> voiceValidator,
        IValidator<UpdateInvoiceRequest> updateValidator)
    {
        _documentService = documentService;
        _manualValidator = manualValidator;
        _voiceValidator = voiceValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Create invoice manually (user fills form)</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentResponse>> CreateManually([FromBody] CreateInvoiceManuallyRequest request, CancellationToken ct)
    {
        var validation = await _manualValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await _documentService.CreateInvoiceManuallyAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Create invoice from voice (transcript or audio)</summary>
    [HttpPost("voice")]
    public async Task<ActionResult<DocumentResponse>> CreateFromVoice([FromBody] CreateInvoiceFromVoiceRequest request, CancellationToken ct)
    {
        var validation = await _voiceValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await _documentService.CreateInvoiceFromVoiceAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get invoice details</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDetailResponse>> GetInvoice(Guid id, CancellationToken ct)
    {
        var result = await _documentService.GetInvoiceAsync(id, ct);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update invoice (draft only)</summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DocumentResponse>> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct)
    {
        if (id != request.InvoiceId)
            return BadRequest(new DocumentResponse { Success = false, Message = "Invoice ID mismatch" });

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return BadRequest(new DocumentResponse
            {
                Success = false,
                Message = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))
            });

        var result = await _documentService.UpdateInvoiceAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>List invoices with filters</summary>
    [HttpGet]
    public async Task<ActionResult<ListDocumentsResponse>> ListInvoices([FromQuery] ListDocumentsRequest request, CancellationToken ct)
    {
        var result = await _documentService.ListDocumentsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Share invoice via WhatsApp, Email, or download link</summary>
    [HttpPost("{id}/share")]
    public async Task<ActionResult<ShareDocumentResponse>> ShareInvoice(Guid id, [FromBody] ShareDocumentRequest request, CancellationToken ct)
    {
        if (id != request.DocumentId)
            return BadRequest(new ShareDocumentResponse { Success = false, Message = "Invoice ID mismatch" });

        var result = await _documentService.ShareDocumentAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
