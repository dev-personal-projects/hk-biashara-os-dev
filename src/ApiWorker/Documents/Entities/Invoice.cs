//Header + ICollection<InvoiceLine>

// src/ApiWorker/Documents/Entities/Invoice.cs
using System;
using System.Collections.Generic;

namespace ApiWorker.Documents.Entities;

/// <summary>
/// Invoice extends Document with customer and line items.
/// </summary>
public sealed class Invoice : Document
{
    // Customer
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    // Billing/Reference
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public string? Reference { get; set; }   // PO ref / external ref
    public string? Notes { get; set; }

    // Discounts (optional)
    public decimal? DiscountRate { get; set; }   // e.g. 0.05 for 5%
    public decimal? DiscountAmount { get; set; } // absolute

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}
