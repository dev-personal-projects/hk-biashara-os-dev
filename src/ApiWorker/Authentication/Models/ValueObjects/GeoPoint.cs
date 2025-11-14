namespace ApiWorker.Authentication.Models.ValueObjects;


/// <summary>Immutable lat/long pair with basic range checks.</summary>

public readonly record struct GeoPoint(decimal Latitude, decimal Longitude)
{
    public static bool IsValid(decimal lat, decimal lng) =>
        lat is >= -90m and <= 90m && lng is >= -180m and <= 180m;

    public static GeoPoint Create(decimal lat, decimal lng)
    {
        if (!IsValid(lat, lng)) throw new ArgumentOutOfRangeException(nameof(lat), "Invalid coordinates.");
        return new GeoPoint(lat, lng);
    }
}