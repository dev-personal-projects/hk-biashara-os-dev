using System;
using System.Collections.Generic;

namespace ApiWorker.Authentication.Entities;

// One tenant (single outlet for MVP).
public sealed class Business : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "retail"; // simple string for MVP

    public string County { get; set; } = string.Empty;
    public string? Town { get; set; }

    public decimal? Latitude { get; set; }   // decimal(9,6)
    public decimal? Longitude { get; set; }  // decimal(9,6)

    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string Currency { get; set; } = "KES";
    public bool UsesVat { get; set; }

    // 0..100; nullable when unused.
    public decimal? DefaultTaxRate { get; set; }

    public Guid? DefaultTemplateId { get; set; }
    public Template? DefaultTemplate { get; set; }

    // Nav
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Template> Templates { get; set; } = new List<Template>();
}
