using System.Security.Claims;
using ApiWorker.Authentication.Entities;
using ApiWorker.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiWorker.Authentication.Middleware;

public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CurrentUserMiddleware> _logger;

    public CurrentUserMiddleware(RequestDelegate next, ILogger<CurrentUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var supabaseUserId = context.User.FindFirst("sub")?.Value;
            
            if (!string.IsNullOrEmpty(supabaseUserId))
            {
                try
                {
                    var user = await db.Users
                        .Include(u => u.Memeberships.Where(m => m.Status == ApiWorker.Authentication.Enum.MembershipStatus.Active))
                        .ThenInclude(m => m.Business)
                        .FirstOrDefaultAsync(u => u.SupabaseUserId == supabaseUserId);

                    if (user != null)
                    {
                        context.Items["UserId"] = user.Id;
                        context.Items["User"] = user;
                        
                        var activeMembership = user.Memeberships.FirstOrDefault();
                        if (activeMembership != null)
                        {
                            context.Items["BusinessId"] = activeMembership.BusinessId;
                            context.Items["Business"] = activeMembership.Business;
                            context.Items["UserRole"] = activeMembership.Role;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load user context for {SupabaseUserId}", supabaseUserId);
                }
            }
        }

        await _next(context);
    }
}