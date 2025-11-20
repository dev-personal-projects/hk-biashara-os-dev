// Template Seeding Utility
// Usage:
//   ./scripts/seed-templates.sh
//   ASPNETCORE_ENVIRONMENT=Production ./scripts/seed-templates.sh

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using ApiWorker.Data;
using ApiWorker.Documents.Entities;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Services;
using ApiWorker.Documents.Settings;
using ApiWorker.Documents.ValueObjects;
using ApiWorker.Storage;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocumentEntityType = ApiWorker.Documents.Entities.DocumentType;
using WordprocessingDocumentRoot = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace ApiWorker.Scripts;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            await TemplateSeeder.SeedTemplatesAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Template seeding failed: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

public sealed class TemplateSeeder
{
    public static async Task SeedTemplatesAsync()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // Build configuration (same as API)
        var appDirectory = Path.Combine(Directory.GetCurrentDirectory(), "src", "ApiWorker");

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        var preliminaryConfig = configBuilder.Build();
        var keyVaultName = preliminaryConfig["KeyVaultName"] ?? "dev-bos-kv";
        if (!string.IsNullOrWhiteSpace(keyVaultName))
        {
            var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
            var credential = new DefaultAzureCredential();
            var secretClient = new SecretClient(keyVaultUri, credential);
            configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

        var configuration = configBuilder.Build();

        // Setup services
        var services = new ServiceCollection();
        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        services.AddOptions<TemplateStorageSettings>()
            .Bind(configuration.GetSection("Documents:Storage"))
            .ValidateDataAnnotations();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is missing.")));

        services.AddSingleton(provider =>
        {
            var connectionString = configuration.GetConnectionString("BlobStorage")
                ?? throw new InvalidOperationException("Connection string 'BlobStorage' is missing.");
            return new Azure.Storage.Blobs.BlobServiceClient(connectionString);
        });

        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<TemplateDocumentGenerator>();
        services.AddScoped<TemplatePreviewGenerator>();
        services.AddScoped<ITemplateService, TemplateService>();

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        var db = scopedProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var blobServiceClient = scopedProvider.GetRequiredService<Azure.Storage.Blobs.BlobServiceClient>();
        var templateService = scopedProvider.GetRequiredService<ITemplateService>();
        var blobStorage = scopedProvider.GetRequiredService<IBlobStorageService>();
        var previewGenerator = scopedProvider.GetRequiredService<TemplatePreviewGenerator>();
        var settings = scopedProvider.GetRequiredService<IOptions<TemplateStorageSettings>>().Value;
        var logger = scopedProvider.GetRequiredService<ILogger<TemplateSeeder>>();

        var docContainer = settings.TemplatesContainer;
        var previewContainer = settings.PreviewsContainer ?? "doc-previews";
        var docsContainer = settings.DocumentsContainer;

        logger.LogInformation("Environment: {Environment}", environment);
        logger.LogInformation("Templates container: {Container}", docContainer);

        await EnsureContainerExistsAsync(blobServiceClient, docContainer, logger);
        await EnsureContainerExistsAsync(blobServiceClient, previewContainer, logger);
        if (!string.IsNullOrWhiteSpace(docsContainer))
            await EnsureContainerExistsAsync(blobServiceClient, docsContainer, logger);

        var templates = new[]
        {
            new { Type = DocumentEntityType.Invoice, Name = "Modern Blue", Theme = new DocumentTheme { PrimaryColor = "#1E40AF", SecondaryColor = "#3B82F6", AccentColor = "#60A5FA", FontFamily = "Inter" }, IsDefault = true },
            new { Type = DocumentEntityType.Invoice, Name = "Classic Green", Theme = new DocumentTheme { PrimaryColor = "#065F46", SecondaryColor = "#047857", AccentColor = "#10B981", FontFamily = "Poppins" }, IsDefault = false },
            new { Type = DocumentEntityType.Receipt, Name = "Elegant Purple", Theme = new DocumentTheme { PrimaryColor = "#6B21A8", SecondaryColor = "#7C3AED", AccentColor = "#A78BFA", FontFamily = "Roboto" }, IsDefault = true },
            new { Type = DocumentEntityType.Receipt, Name = "Bold Orange", Theme = new DocumentTheme { PrimaryColor = "#C2410C", SecondaryColor = "#EA580C", AccentColor = "#FB923C", FontFamily = "Open Sans" }, IsDefault = false },
            new { Type = DocumentEntityType.Quotation, Name = "Professional Gray", Theme = new DocumentTheme { PrimaryColor = "#374151", SecondaryColor = "#4B5563", AccentColor = "#6B7280", FontFamily = "Lato" }, IsDefault = true },
            new { Type = DocumentEntityType.Quotation, Name = "Vibrant Teal", Theme = new DocumentTheme { PrimaryColor = "#134E4A", SecondaryColor = "#0F766E", AccentColor = "#14B8A6", FontFamily = "Montserrat" }, IsDefault = false }
        };

        foreach (var templateDef in templates)
        {
            try
            {
                logger.LogInformation("Processing template: {Name} ({Type})", templateDef.Name, templateDef.Type);

                var exists = await db.DocumentTemplates
                    .AnyAsync(t => t.BusinessId == null && t.Type == templateDef.Type && t.Name == templateDef.Name);

                if (exists)
                {
                    logger.LogInformation("Template {Name} already exists. Skipping.", templateDef.Name);
                    continue;
                }

                await using var docxStream = GenerateTemplateDocx(templateDef.Name, templateDef.Theme);

                var blobFileName = $"global/{templateDef.Type}/{templateDef.Name.ToLower().Replace(" ", "-")}-v1.docx";
                var blobUrl = await blobStorage.UploadAsync(
                    docxStream,
                    blobFileName,
                    docContainer,
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    CancellationToken.None);

                var template = new Template
                {
                    Id = Guid.NewGuid(),
                    BusinessId = null,
                    Type = templateDef.Type,
                    Name = templateDef.Name,
                    Version = 1,
                    BlobPath = blobFileName,
                    ThemeJson = templateDef.Theme.ToJson(),
                    IsDefault = templateDef.IsDefault,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                db.DocumentTemplates.Add(template);
                await db.SaveChangesAsync();

                var previewBytes = await previewGenerator.GeneratePreviewAsync(template);
                await using var previewStream = new MemoryStream(previewBytes);
                var previewFileName = $"{template.Id}.png";
                template.PreviewBlobUrl = await blobStorage.UploadAsync(
                    previewStream,
                    previewFileName,
                    previewContainer,
                    "image/png",
                    CancellationToken.None);

                await db.SaveChangesAsync();

                if (templateDef.IsDefault)
                {
                    await templateService.SetDefaultTemplateAsync(template.Id);
                }

                logger.LogInformation("Template {Name} created. DOCX: {DocxUrl} Preview: {Preview}", templateDef.Name, blobUrl, template.PreviewBlobUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process template {Name}", templateDef.Name);
            }
        }

        logger.LogInformation("Template seeding finished.");
    }

    private static async Task EnsureContainerExistsAsync(Azure.Storage.Blobs.BlobServiceClient client, string containerName, ILogger logger)
    {
        var container = client.GetBlobContainerClient(containerName);
        var result = await container.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
        if (result != null)
            logger.LogInformation("Created missing blob container '{ContainerName}'.", containerName);
    }

    private static MemoryStream GenerateTemplateDocx(string name, DocumentTheme theme)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new WordprocessingDocumentRoot(new Body());
            var body = mainPart.Document.Body!;

            body.AppendChild(CreateParagraph($"{name} Template", bold: true, fontSize: "24", color: theme.PrimaryColor));
            body.AppendChild(CreateParagraph("Business Information", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("Business Name: {BusinessName}"));
            body.AppendChild(CreateParagraph("Phone: {BusinessPhone}"));
            body.AppendChild(CreateParagraph("Email: {BusinessEmail}"));
            body.AppendChild(CreateParagraph("Address: {BusinessAddress}"));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Document Details", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("Type: {DocumentType}"));
            body.AppendChild(CreateParagraph("Number: {DocumentNumber}"));
            body.AppendChild(CreateParagraph("Date: {DocumentDate}"));
            body.AppendChild(CreateParagraph("Due Date: {DueDate}"));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Customer Information", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("Name: {CustomerName}"));
            body.AppendChild(CreateParagraph("Phone: {CustomerPhone}"));
            body.AppendChild(CreateParagraph("Email: {CustomerEmail}"));
            body.AppendChild(CreateParagraph("Address: {CustomerAddress}"));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Line Items:", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("{LineItems}"));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Totals", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("Subtotal: {Subtotal}"));
            body.AppendChild(CreateParagraph("Tax: {Tax}"));
            body.AppendChild(CreateParagraph("Total: {Total}", bold: true, fontSize: "20", color: theme.AccentColor));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Notes: {Notes}"));
            body.AppendChild(CreateParagraph("Reference: {Reference}"));
            body.AppendChild(new Paragraph());

            body.AppendChild(CreateParagraph("Signature:", bold: true, fontSize: "18", color: theme.SecondaryColor));
            body.AppendChild(CreateParagraph("{Signature}"));
        }

        stream.Position = 0;
        return stream;
    }

    private static Paragraph CreateParagraph(string text, bool bold = false, string fontSize = "12", string? color = null)
    {
        var para = new Paragraph();
        var run = new Run();
        var runProps = new RunProperties();

        if (bold)
            runProps.AppendChild(new Bold());

        runProps.AppendChild(new FontSize { Val = fontSize });

        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new Color { Val = NormalizeColor(color) });

        if (runProps.ChildElements.Count > 0)
            run.AppendChild(runProps);

        run.AppendChild(new Text(text));
        para.AppendChild(run);
        return para;
    }

    private static string NormalizeColor(string? color, string fallback = "000000")
    {
        if (string.IsNullOrWhiteSpace(color))
            return fallback;

        var hex = color.Trim();
        if (hex.StartsWith("#"))
            hex = hex[1..];

        return hex.Length == 6 ? hex.ToUpperInvariant() : fallback;
    }
}