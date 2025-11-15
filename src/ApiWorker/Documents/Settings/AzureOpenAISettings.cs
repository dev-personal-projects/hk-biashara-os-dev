namespace ApiWorker.Documents.Settings;

public sealed class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Deployment { get; set; } = "gpt-4o-mini";
}
