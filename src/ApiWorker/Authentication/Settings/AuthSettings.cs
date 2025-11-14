namespace ApiWorker.Authentication.Settings;

public sealed class AuthSettings
{
    public SupabaseConfig Supabase { get; init; } = new();
    public JwtConfig Jwt { get; init; } = new();
    public Defaults DefaultValues { get; init; } = new();

    public sealed class SupabaseConfig
    {
        public string Url { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
    }

    public sealed class JwtConfig
    {
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string JwksUrl { get; init; } = string.Empty;
        public bool RequireHttpsMetadata { get; init; } = true;
    }

    public sealed class Defaults
    {
        public string Currency { get; init; } = "KES";
        public bool RequireMfaForOwners { get; init; } = false;
    }
}
