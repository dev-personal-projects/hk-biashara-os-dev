using ApiWorker.Authentication.Models.BusinessLogic;

namespace ApiWorker.Authentication.Models.ReadModels;

public sealed class UserSessionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public UserSession? User { get; init; }
}