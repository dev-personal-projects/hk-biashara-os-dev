

using ApiWorker.Authentication.Models.ReadModels;


/// <summary>What the app needs immediately after sign-in.</summary>
public sealed record AuthBootstrapResult(
    AuthUserSummary User,
    IReadOnlyList<BusinessSummary> Businesses,
    bool NeedsBusinessSetup // true when user has no business membership yet
);