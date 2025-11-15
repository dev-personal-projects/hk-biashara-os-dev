using ApiWorker.Authentication.Entities;
using ApiWorker.Documents.Entities;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Renders invoices to DOCX and PDF formats.
/// </summary>
public interface IRenderService
{
    /// <summary>Generates editable DOCX file</summary>
    Task<Stream> RenderDocxAsync(Invoice invoice, Business business, CancellationToken ct = default);

    /// <summary>Generates final PDF file</summary>
    Task<Stream> RenderPdfAsync(Invoice invoice, Business business, CancellationToken ct = default);
}
