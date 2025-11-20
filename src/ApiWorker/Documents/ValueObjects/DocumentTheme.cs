using System.Text.Json;

namespace ApiWorker.Documents.ValueObjects;

/// <summary>
/// Represents styling options applied to rendered documents.
/// Stored as JSON on the document row so previews can be re-created consistently.
/// </summary>
public sealed class DocumentTheme
{
    public string PrimaryColor { get; set; } = "#111827";   // Dark title text
    public string SecondaryColor { get; set; } = "#1F2937"; // Section headings
    public string AccentColor { get; set; } = "#F97316";    // Totals + highlights
    public string FontFamily { get; set; } = "Poppins";

    public static DocumentTheme Default => new();

    public static DocumentTheme FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Default;

        try
        {
            var theme = JsonSerializer.Deserialize<DocumentTheme>(json);
            return theme ?? Default;
        }
        catch
        {
            return Default;
        }
    }

    public static DocumentTheme FromDto(ApiWorker.Documents.DTOs.DocumentThemeDto? dto)
    {
        if (dto == null)
            return Default;

        return new DocumentTheme
        {
            PrimaryColor = string.IsNullOrWhiteSpace(dto.PrimaryColor) ? Default.PrimaryColor : dto.PrimaryColor,
            SecondaryColor = string.IsNullOrWhiteSpace(dto.SecondaryColor) ? Default.SecondaryColor : dto.SecondaryColor,
            AccentColor = string.IsNullOrWhiteSpace(dto.AccentColor) ? Default.AccentColor : dto.AccentColor,
            FontFamily = string.IsNullOrWhiteSpace(dto.FontFamily) ? Default.FontFamily : dto.FontFamily
        };
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public ApiWorker.Documents.DTOs.DocumentThemeDto ToDto() => new()
    {
        PrimaryColor = PrimaryColor,
        SecondaryColor = SecondaryColor,
        AccentColor = AccentColor,
        FontFamily = FontFamily
    };
}

