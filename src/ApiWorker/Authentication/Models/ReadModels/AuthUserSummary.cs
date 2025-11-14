using ApiWorker.Authentication.Models.ValueObjects;

namespace ApiWorker.Authentication.Models.ReadModels;

/// <summary>Minimal profile returned to the client after auth/bootstrap.</summary>
public sealed record AuthUserSummary(
    Guid UserId,
    string FullName,
    string Email,
    string County,          // display value (e.g., "Kiambu")
    GeoPoint? Location      // null if unknown
);