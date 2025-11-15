using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Services;
using ApiWorker.Speech.Settings;

namespace ApiWorker.Speech.Extensions;

/// <summary>
/// Extension methods for registering Speech module services in DI container.
/// Handles both low-level STT and high-level capture orchestration.
/// </summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>
    /// Registers Speech-to-Text services and settings.
    /// Call this in Program.cs: builder.Services.AddSpeechInfrastructure(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddSpeechInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ===== SETTINGS REGISTRATION =====
        
        /// <summary>
        /// Speech settings: Azure region, key, locales, phrase lists.
        /// Binds from appsettings.json section "Speech".
        /// </summary>
        services.AddOptions<SpeechSettings>()
            .Bind(configuration.GetSection("Speech"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        /// <summary>
        /// Voice settings: Azure Speech SDK configuration for voice features.
        /// Binds from appsettings.json section "Voice".
        /// </summary>
        services.AddOptions<VoiceSettings>()
            .Bind(configuration.GetSection("Voice"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ===== SERVICE REGISTRATION =====
        
        /// <summary>
        /// ISpeechToTextService: Low-level Azure Speech SDK wrapper.
        /// Scoped lifetime: New instance per HTTP request (safe for concurrent requests).
        /// </summary>
        services.AddScoped<ISpeechToTextService, AzureSpeechToTextService>();
        
        return services;
    }
}
