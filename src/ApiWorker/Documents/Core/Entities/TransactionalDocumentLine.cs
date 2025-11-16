namespace ApiWorker.Documents.Entities;

/// <summary>
/// Line item for transactional documents (Invoice, Receipt, Quotation).
/// </summary>
public sealed class TransactionalDocumentLine
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public TransactionalDocument Document { get; set; } = default!;
}
