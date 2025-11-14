using ApiWorker.Documents.DTOs;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Shares documents via WhatsApp, Email, or download links.
/// </summary>
public interface IShareService
{
    /// <summary>Sends document via WhatsApp</summary>
    Task<ShareDocumentResponse> ShareViaWhatsAppAsync(Guid documentId, string phoneNumber, string? message = null, CancellationToken ct = default);
    
    /// <summary>Sends document via Email</summary>
    Task<ShareDocumentResponse> ShareViaEmailAsync(Guid documentId, string email, string? message = null, CancellationToken ct = default);
    
    /// <summary>Generates public download link</summary>
    Task<ShareDocumentResponse> GenerateDownloadLinkAsync(Guid documentId, CancellationToken ct = default);
}
