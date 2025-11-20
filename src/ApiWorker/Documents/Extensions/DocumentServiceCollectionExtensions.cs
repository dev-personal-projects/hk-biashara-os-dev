using ApiWorker.Documents.Settings;
using ApiWorker.Documents.Services;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Mappings;
using FluentValidation;

namespace ApiWorker.Documents.Extensions;

/// <summary>
/// Extension methods for registering Documents module services in DI container.
/// This centralizes all Documents-related service registration for clean Program.cs.
/// </summary>
public static class DocumentServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Documents module services and settings.
    /// Call this in Program.cs: builder.Services.AddDocumentsInfrastructure(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddDocumentsInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // ===== SETTINGS REGISTRATION =====
        // Options pattern: Binds configuration sections to strongly-typed classes
        // ValidateDataAnnotations: Ensures [Required], [Range], etc. are enforced
        // ValidateOnStart: Fails fast at startup if configuration is invalid (better than runtime errors)
        
        /// <summary>
        /// Document settings: Default currency, locale, numbering patterns.
        /// Binds from appsettings.json section "Documents".
        /// </summary>
        services.AddOptions<DocumentSettings>()
            .Bind(configuration.GetSection("Documents"))
            .ValidateDataAnnotations()  // Validates [Required], [StringLength], etc.
            .ValidateOnStart();         // Fails at startup if invalid (not at first use)

        /// <summary>
        /// Template storage settings: Blob container names, path formats.
        /// Binds from appsettings.json section "Documents:Storage".
        /// </summary>
        services.AddOptions<TemplateStorageSettings>()
            .Bind(configuration.GetSection("Documents:Storage"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AzureOpenAISettings>()
            .Bind(configuration.GetSection("AzureOpenAI"));

        // ===== AUTOMAPPER REGISTRATION =====
        services.AddAutoMapper(typeof(DocumentMappingProfile));

        // ===== FLUENTVALIDATION REGISTRATION =====
        services.AddValidatorsFromAssemblyContaining<DocumentMappingProfile>();

        // ===== SERVICE REGISTRATION =====
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<TemplateDocumentGenerator>();
        services.AddScoped<TemplatePreviewGenerator>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<IVoiceIntentService, VoiceIntentService>();

        return services;
    }
}
