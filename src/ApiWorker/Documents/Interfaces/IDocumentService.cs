using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Core service for invoice operations (create, update, list, share).
/// </summary>
public interface IDocumentService
{
    // Create
    Task<DocumentResponse> CreateInvoiceFromVoiceAsync(CreateInvoiceFromVoiceRequest request, CancellationToken ct = default);
    Task<DocumentResponse> CreateInvoiceManuallyAsync(CreateInvoiceManuallyRequest request, CancellationToken ct = default);

    // Read
    Task<InvoiceDetailResponse> GetInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
    Task<ListDocumentsResponse> ListDocumentsAsync(ListDocumentsRequest request, CancellationToken ct = default);

    // Update
    Task<DocumentResponse> UpdateInvoiceAsync(UpdateInvoiceRequest request, CancellationToken ct = default);

    // Share
    Task<ShareDocumentResponse> ShareDocumentAsync(ShareDocumentRequest request, CancellationToken ct = default);
}
