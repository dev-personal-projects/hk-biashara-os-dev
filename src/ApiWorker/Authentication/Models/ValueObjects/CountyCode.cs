using System.Globalization;

namespace ApiWorker.Authentication.Models.ValueObjects;

/// <summary>Kenya county value object (string-backed with whitelist).</summary>


/// <summary>Kenya county value object (string-backed with whitelist).</summary>
public sealed class CountyCode
{
    public string Value { get; }

    private CountyCode(string value) => Value = value;

    public static bool IsValid(string? input)
        => input is not null && ValidCounties.Contains(Normalize(input));

    public static CountyCode From(string input)
    {
        var norm = Normalize(input);
        if (!ValidCounties.Contains(norm)) throw new ArgumentException("Unknown county.", nameof(input));
        return new CountyCode(norm);
    }

    public override string ToString() => Value;

    static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        // TitleCase for consistent display (e.g., "kiambu" -> "Kiambu")
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);
    }

    // 47 counties (canonical display names)
    static readonly HashSet<string> ValidCounties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mombasa","Kwale","Kilifi","Tana River","Lamu","Taita Taveta",
        "Garissa","Wajir","Mandera","Marsabit","Isiolo","Meru","Tharaka-Nithi",
        "Embu","Kitui","Machakos","Makueni","Nyandarua","Nyeri","Kirinyaga",
        "Murang'a","Kiambu","Turkana","West Pokot","Samburu","Trans Nzoia",
        "Uasin Gishu","Elgeyo-Marakwet","Nandi","Baringo","Laikipia","Nakuru",
        "Narok","Kajiado","Kericho","Bomet","Kakamega","Vihiga","Bungoma",
        "Busia","Siaya","Kisumu","Homa Bay","Migori","Kisii","Nyamira","Nairobi"
    };
}