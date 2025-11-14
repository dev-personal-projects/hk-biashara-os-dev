using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiWorker.Authentication.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireBusinessProfileAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var businessId = context.HttpContext.Items["BusinessId"] as Guid?;
        if (!businessId.HasValue)
        {
            context.Result = new ObjectResult(new { message = "Business profile required" })
            {
                StatusCode = 403
            };
        }
    }
}