using ApiWorker.Documents.DTOs;

namespace ApiWorker.Documents.Interfaces;

public interface IVoiceIntentService
{
    Task<ExtractedInvoiceData?> ExtractInvoiceDataAsync(string transcript, string locale, CancellationToken ct = default);
}
