using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Enum;

namespace ApiWorker.Authentication.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? BusinessId { get; }
    MembershipRole? UserRole { get; }
    AppUser? User { get; }
    Business? Business { get; }
    bool IsAuthenticated { get; }
    bool HasBusiness { get; }   
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId => _httpContextAccessor.HttpContext?.Items["UserId"] as Guid?;
    public Guid? BusinessId => _httpContextAccessor.HttpContext?.Items["BusinessId"] as Guid?;
    public MembershipRole? UserRole => _httpContextAccessor.HttpContext?.Items["UserRole"] as MembershipRole?;
    public AppUser? User => _httpContextAccessor.HttpContext?.Items["User"] as AppUser;
    public Business? Business => _httpContextAccessor.HttpContext?.Items["Business"] as Business;
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    public bool HasBusiness => BusinessId.HasValue;
}