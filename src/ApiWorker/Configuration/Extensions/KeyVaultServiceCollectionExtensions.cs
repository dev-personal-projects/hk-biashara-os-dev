using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

namespace ApiWorker.Configuration.Extensions;

public static class KeyVaultServiceCollectionExtensions
{
    public static WebApplicationBuilder AddKeyVaultConfiguration(this WebApplicationBuilder builder)
    {
        var keyVaultName = builder.Configuration["KeyVaultName"] ?? "dev-bos-kv";
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        var credential = new DefaultAzureCredential();
        var secretClient = new SecretClient(keyVaultUri, credential);
        
        builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        
        return builder;
    }
}
