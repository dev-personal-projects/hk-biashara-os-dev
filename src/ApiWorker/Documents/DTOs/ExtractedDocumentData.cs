namespace ApiWorker.Documents.DTOs;

/// <summary>
/// Data extracted from voice input for document creation.
/// Used for Invoice, Receipt, and Quotation.
/// </summary>
public sealed class ExtractedDocumentData
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public List<ExtractedLineItem> Items { get; set; } = new();
    public string? Notes { get; set; }
}

public sealed class ExtractedLineItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
