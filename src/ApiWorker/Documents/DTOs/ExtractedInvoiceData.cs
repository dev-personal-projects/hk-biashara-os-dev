namespace ApiWorker.Documents.DTOs;

public sealed class ExtractedInvoiceData
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
