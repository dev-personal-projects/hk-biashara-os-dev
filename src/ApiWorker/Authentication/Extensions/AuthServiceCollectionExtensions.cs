using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ApiWorker.Authentication.Interfaces;
using ApiWorker.Authentication.Services;
using ApiWorker.Authentication.Settings;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace ApiWorker.Authentication.Extensions;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("Auth:Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Missing Auth:Jwt settings.");

        var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    
                    TryAllIssuerSigningKeys = true
                };
            });

        services.AddAuthorization();

        // Register auth services
        services.Configure<AuthSettings>(config.GetSection("Auth"));
        services.Configure<JwtSettings>(config.GetSection("Auth:Jwt"));
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Register Supabase
        var supabaseSettings = config.GetSection("Auth:Supabase").Get<AuthSettings.SupabaseConfig>()
            ?? throw new InvalidOperationException("Missing Auth:Supabase settings.");

        services.AddScoped<IGotrueClient<User, Session>>(_ => 
            new Supabase.Gotrue.Client(new Supabase.Gotrue.ClientOptions
            {
                Url = $"{supabaseSettings.Url}/auth/v1",
                Headers = new Dictionary<string, string>
                {
                    { "apikey", supabaseSettings.Key }
                }
            }));

        return services;
    }

    public sealed class JwtSettings
    {
        public string SecretKey { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpiryHours { get; init; } = 24;
    }
}