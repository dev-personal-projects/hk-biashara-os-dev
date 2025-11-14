using ApiWorker.Speech.Interfaces;
using ApiWorker.Speech.Services;
using ApiWorker.Speech.Settings;

namespace ApiWorker.Speech.Extensions;

public static class SpeechServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<SpeechSettings>(config.GetSection("Speech"));
        services.AddScoped<ISpeechToTextService, AzureSpeechToTextService>();
        
        return services;
    }
}
