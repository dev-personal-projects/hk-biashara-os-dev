using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWorker.Documents.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>List all documents with filters</summary>
    [HttpGet]
    public async Task<ActionResult<ListDocumentsResponse>> ListDocuments([FromQuery] ListDocumentsRequest request, CancellationToken ct)
    {
        var result = await _documentService.ListDocumentsAsync(request, ct);
        return Ok(result);
    }

    /// <summary>Share document via WhatsApp, Email, or download link</summary>
    [HttpPost("{id}/share")]
    public async Task<ActionResult<ShareDocumentResponse>> ShareDocument(Guid id, [FromBody] ShareDocumentRequest request, CancellationToken ct)
    {
        if (id != request.DocumentId)
            return BadRequest(new ShareDocumentResponse { Success = false, Message = "Document ID mismatch" });

        var result = await _documentService.ShareDocumentAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}