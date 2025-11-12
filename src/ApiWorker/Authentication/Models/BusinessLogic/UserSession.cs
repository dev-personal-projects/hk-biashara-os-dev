using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Enum;

namespace ApiWorker.Authentication.Models.BusinessLogic;

public sealed class UserSession
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public BusinessContext? Business { get; init; }
    public bool HasBusiness => Business != null;

    public static UserSession FromUser(AppUser user, Membership? membership = null)
    {
        BusinessContext? businessContext = null;
        
        if (membership?.Business != null)
        {
            businessContext = BusinessContext.FromBusiness(membership.Business, membership.Role);
        }

        return new UserSession
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            County = user.County,
            Business = businessContext
        };
    }
}