using System.Text.Json;
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

// Registers JWT bearer validation against Supabase JWKS and basic authorization.
// In Program.cs: services.AddAuthenticationInfrastructure(builder.Configuration);

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Auth:SupabaseJwt").Get<SupabaseJwtSettings>()
                  ?? throw new InvalidOperationException("Missing Auth:SupabaseJwt settings.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
                options.SaveToken = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                        JwksCache.GetKeys(jwt.JwksUrl)
                };
            });

        services.AddAuthorization(); // default: any authenticated user

        // Register auth services
        services.Configure<AuthSettings>(config.GetSection("Auth"));
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

    // Tiny in-memory JWKS cache (24h). Keeps things simple for MVP.
    private static class JwksCache
    {
        private static readonly HttpClient _http = new();
        private static DateTimeOffset _fetchedAt = DateTimeOffset.MinValue;
        private static JsonWebKeySet? _jwks;

        public static IEnumerable<SecurityKey> GetKeys(string jwksUrl)
        {
            if (_jwks is null || DateTimeOffset.UtcNow - _fetchedAt > TimeSpan.FromHours(24))
            {
                var json = _http.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
                _jwks = new JsonWebKeySet(json);
                _fetchedAt = DateTimeOffset.UtcNow;
            }
            return _jwks!.Keys;
        }
    }

    // Settings POCO lives here for convenience
    public sealed class SupabaseJwtSettings
    {
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string JwksUrl { get; init; } = string.Empty;
        public bool RequireHttpsMetadata { get; init; } = true;
    }
}