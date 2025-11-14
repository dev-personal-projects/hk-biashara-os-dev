using System;
using ApiWorker.Authentication.Enum;

namespace ApiWorker.Authentication.Entities;

// Document layout stored as JSON; one default per doc-type per business.
public sealed class Template : Entity
{
    // Null => global/seeded template.
    public Guid? BusinessId { get; set; }
    public Business? Business { get; set; }

    public string Name { get; set; } = "Classic";
    public DocType DocType { get; set; } = DocType.Invoice;

    // NVARCHAR(MAX) containing JSON; validated with ISJSON() constraint.
    public string JsonDefinition { get; set; } = "{}";

    public bool IsDefault { get; set; }
}
