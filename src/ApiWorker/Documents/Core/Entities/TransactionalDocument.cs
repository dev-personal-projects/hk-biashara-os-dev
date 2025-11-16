namespace ApiWorker.Documents.Entities;

/// <summary>
/// Base class for transactional documents (Invoice, Receipt, Quotation).
/// Shares customer info, line items, and payment terms.
/// </summary>
public abstract class TransactionalDocument : Document
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public decimal? DiscountRate { get; set; }
    public decimal? DiscountAmount { get; set; }

    public ICollection<TransactionalDocumentLine> Lines { get; set; } = new List<TransactionalDocumentLine>();
}
