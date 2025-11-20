using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using ApiWorker.Documents.DTOs;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Settings;
using Microsoft.Extensions.Options;

namespace ApiWorker.Documents.Services;

public sealed class VoiceIntentService : IVoiceIntentService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<VoiceIntentService> _logger;

    public VoiceIntentService(IOptions<AzureOpenAISettings> settings, ILogger<VoiceIntentService> logger)
    {
        var client = new AzureOpenAIClient(new Uri(settings.Value.Endpoint), new AzureKeyCredential(settings.Value.ApiKey));
        _chatClient = client.GetChatClient(settings.Value.Deployment);
        _logger = logger;
    }

    public async Task<ExtractedDocumentData?> ExtractDocumentDataAsync(string transcript, string locale, CancellationToken ct = default)
    {
        try
        {
            var systemPrompt = locale.StartsWith("sw")
                ? "Wewe ni msaidizi wa biashara. Toa taarifa za hati kutoka kwa maneno ya mtumiaji. Rudisha JSON tu."
                : "You are a business assistant. Extract document details from user speech. Return only JSON.";

            var userPrompt = $@"Extract document data from: ""{transcript}""

Return JSON with this structure:
{{
  ""customerName"": ""string or null"",
  ""customerPhone"": ""string or null"",
  ""items"": [
    {{
      ""name"": ""string"",
      ""quantity"": number,
      ""unitPrice"": number
    }}
  ],
  ""notes"": ""string or null""
}}

Rules:
- If customer not mentioned, set null
- Parse quantities and prices as numbers
- Common items: Unga (flour), Sukuma (kale), Mchele (rice)
- Prices in KES unless specified";

            var response = await _chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                ],
                new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 500,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                },
                ct);

            var content = response.Value.Content[0].Text;

            var extracted = JsonSerializer.Deserialize<ExtractedDocumentData>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return extracted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract document data from transcript");
            return null;
        }
    }
}


