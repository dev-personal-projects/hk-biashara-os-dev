# Speech Module

Azure Speech-to-Text integration for BiasharaOS.

## Structure

```
Speech/
├── DTOs/
│   └── SpeechDtos.cs              # TranscriptionResult
├── Interfaces/
│   └── ISpeechToTextService.cs    # STT contract
├── Services/
│   └── AzureSpeechToTextService.cs # Azure Speech SDK implementation
├── Settings/
│   └── SpeechSettings.cs          # Configuration model
└── Extensions/
    └── SpeechServiceCollectionExtensions.cs # DI registration
```

## Usage

### Configuration (appsettings.json)
```json
{
  "Speech": {
    "Provider": "AzureSpeech",
    "Region": "eastus",
    "Key": "your-azure-speech-key",
    "DefaultLocale": "en-KE",
    "Locales": ["en-KE", "sw-KE"],
    "UseNumeralNormalization": true
  }
}
```

### Dependency Injection
```csharp
// Program.cs
builder.Services.AddSpeechServices(builder.Configuration);
```

### Service Usage
```csharp
public class MyService
{
    private readonly ISpeechToTextService _speech;

    public MyService(ISpeechToTextService speech)
    {
        _speech = speech;
    }

    public async Task<string> TranscribeAudio(Stream audioStream)
    {
        var result = await _speech.TranscribeAsync(audioStream, "sw-KE");
        return result.Success ? result.Text : result.Error ?? "Failed";
    }
}
```

## Supported Locales
- `en-KE` - English (Kenya)
- `sw-KE` - Swahili (Kenya)

## Features
- Stream-based transcription
- File-based transcription
- Locale selection
- Numeral normalization
- Error handling
