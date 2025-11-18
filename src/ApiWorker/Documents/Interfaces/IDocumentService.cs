using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Core service for document operations (Invoice, Receipt, Quotation).
/// Handles: Create (voice/manual), Update, List, Render.
/// </summary>
public interface IDocumentService
{
    // Create
    Task<DocumentResponse> CreateDocumentFromVoiceAsync(CreateDocumentFromVoiceRequest request, CancellationToken ct = default);
    Task<DocumentResponse> CreateDocumentManuallyAsync(CreateDocumentManuallyRequest request, CancellationToken ct = default);

    // Read
    Task<DocumentDetailResponse> GetDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<ListDocumentsResponse> ListDocumentsAsync(ListDocumentsRequest request, CancellationToken ct = default);

    // Update
    Task<DocumentResponse> UpdateDocumentAsync(UpdateDocumentRequest request, CancellationToken ct = default);
}
