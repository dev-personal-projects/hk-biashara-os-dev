//# Name, Label, Type, Required, MapTo

// src/ApiWorker/Documents/Entities/TemplateField.cs
namespace ApiWorker.Documents.Entities;

/// <summary>
/// Value object used inside template JSON definition.
/// </summary>
public sealed class TemplateField
{
    public string Name { get; set; } = string.Empty;     // internal key
    public string Label { get; set; } = string.Empty;    // UI label
    public string Type { get; set; } = "text";           // text, number, date, etc.
    public bool Required { get; set; } = false;
    public string? MapTo { get; set; }                   // path to entity property
}
