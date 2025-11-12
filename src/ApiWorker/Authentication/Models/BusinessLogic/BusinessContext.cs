using ApiWorker.Authentication.Entities;
using ApiWorker.Authentication.Enum;

namespace ApiWorker.Authentication.Models.BusinessLogic;

public sealed class BusinessContext
{
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public string Currency { get; init; } = "KES";
    public bool UsesVat { get; init; }
    public decimal? DefaultTaxRate { get; init; }
    public MembershipRole UserRole { get; init; }

    public static BusinessContext FromBusiness(Business business, MembershipRole userRole)
    {
        return new BusinessContext
        {
            BusinessId = business.Id,
            BusinessName = business.Name,
            Category = business.Category,
            County = business.County,
            Currency = business.Currency,
            UsesVat = business.UsesVat,
            DefaultTaxRate = business.DefaultTaxRate,
            UserRole = userRole
        };
    }
}