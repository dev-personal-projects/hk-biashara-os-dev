

using ApiWorker.Authentication.Models.ValueObjects;

namespace ApiWorker.Authentication.Models.ReadModels;

/// <summary>Lightweight business snapshot for pickers/switchers.</summary>
public sealed record BusinessSummary(
    Guid BusinessId,
    string Name,
    string Category,
    string County,
    GeoPoint? Location
);