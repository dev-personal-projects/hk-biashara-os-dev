using ApiWorker.Documents.Entities;
using ApiWorker.Documents.Interfaces;
using ApiWorker.Documents.Settings;
using ApiWorker.Data;
using ApiWorker.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DocumentTemplate = ApiWorker.Documents.Entities.Template;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Service for managing document templates (global templates only for now).
/// Handles listing, retrieving, and uploading templates.
/// </summary>
public sealed class TemplateService : ITemplateService
{
    private readonly ApplicationDbContext _db;
    private readonly IBlobStorageService _blobStorage;
    private readonly TemplateStorageSettings _settings;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ApplicationDbContext db,
        IBlobStorageService blobStorage,
        IOptions<TemplateStorageSettings> settings,
        ILogger<TemplateService> logger)
    {
        _db = db;
        _blobStorage = blobStorage;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<DocumentTemplate?> GetDefaultTemplateAsync(Guid businessId, DocumentType type, CancellationToken ct = default)
    {
        try
        {
            // For global templates, businessId is ignored - we only return global templates
            var template = await _db.DocumentTemplates
                .FirstOrDefaultAsync(
                    t => t.BusinessId == null && t.Type == type && t.IsDefault,
                    ct);

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default template for type {Type}", type);
            return null;
        }
    }

    public async Task<List<DocumentTemplate>> ListTemplatesAsync(Guid businessId, DocumentType? type = null, CancellationToken ct = default)
    {
        try
        {
            // Only return global templates (BusinessId = null)
            var query = _db.DocumentTemplates
                .Where(t => t.BusinessId == null);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            var templates = await query
                .OrderBy(t => t.Type)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list templates");
            return new List<Template>();
        }
    }

    public async Task<DocumentTemplate?> GetTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        try
        {
            var template = await _db.DocumentTemplates
                .FirstOrDefaultAsync(t => t.Id == templateId && t.BusinessId == null, ct);

            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", templateId);
            return null;
        }
    }

    public async Task<DocumentTemplate> UploadTemplateAsync(Guid businessId, DocumentType type, string name, Stream templateFile, CancellationToken ct = default)
    {
        // For global templates, businessId should be null
        var isGlobal = businessId == Guid.Empty;

        var fileName = $"{name.ToLowerInvariant().Replace(" ", "-")}-v1.docx";
        var blobPath = isGlobal 
            ? $"global/{type}/{fileName}"
            : $"{businessId}/{type}/{fileName}";

        // Upload to blob storage
        var blobUrl = await _blobStorage.UploadAsync(
            templateFile,
            blobPath,
            _settings.TemplatesContainer,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ct);

        var template = new DocumentTemplate
        {
            Id = Guid.NewGuid(),
            BusinessId = isGlobal ? null : businessId,
            Type = type,
            Name = name,
            Version = 1,
            BlobPath = blobPath,
            IsDefault = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.DocumentTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Template uploaded: {Name} ({Type})", name, type);

        return template;
    }

    public async Task SetDefaultTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = await _db.DocumentTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template == null)
            throw new InvalidOperationException($"Template {templateId} not found");

        // Unset all other defaults for this type (global templates only)
        var otherDefaults = await _db.DocumentTemplates
            .Where(t => t.BusinessId == null && t.Type == template.Type && t.IsDefault && t.Id != templateId)
            .ToListAsync(ct);

        foreach (var other in otherDefaults)
        {
            other.IsDefault = false;
            other.UpdatedAt = DateTimeOffset.UtcNow;
        }

        template.IsDefault = true;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Template {TemplateId} set as default for type {Type}", templateId, template.Type);
    }
}
