using ApiWorker.Authentication.DTOS;
using ApiWorker.Authentication.Models.ReadModels;

namespace ApiWorker.Authentication.Interfaces;

public interface IAuthenticationService
{
    Task<SignupResponse> CreateUserAccountAsync(SignupRequest request, CancellationToken ct);
    Task<LoginResponse> AuthenticateUserAsync(LoginRequest request, CancellationToken ct);
    Task<LoginResponse> AuthenticateWithGoogleAsync(GoogleAuthRequest request, CancellationToken ct);
    Task<RegisterBusinessResponse> CreateBusinessProfileAsync(Guid userId, RegisterBusinessRequest request, CancellationToken ct);
    Task<UserSessionResult> InitializeUserSessionAsync(InitializeSessionRequest request, CancellationToken ct);
}
