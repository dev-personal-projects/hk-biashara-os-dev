// src/ApiWorker/Documents/Entities/InvoiceLine.cs
using System;

namespace ApiWorker.Documents.Entities;

/// <summary>
/// Single invoice line.
/// </summary>
public sealed class InvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }     // FK -> Invoice.Id

    public string Name { get; set; } = string.Empty;     // item/service
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1m;          // 18,3
    public decimal UnitPrice { get; set; }               // 18,2
    public decimal TaxRate { get; set; }                 // e.g. 0.16 VAT
    public decimal LineTotal { get; set; }               // stored for speed

    // Navigation
    public Invoice Invoice { get; set; } = default!;
}
