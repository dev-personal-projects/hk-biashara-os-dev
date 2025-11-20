using ApiWorker.Documents.DTOs;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Service for extracting document data from voice transcripts.
/// Works for Invoice, Receipt, and Quotation.
/// </summary>
public interface IVoiceIntentService
{
    Task<ExtractedDocumentData?> ExtractDocumentDataAsync(string transcript, string locale, CancellationToken ct = default);
}
