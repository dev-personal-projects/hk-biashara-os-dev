using ApiWorker.Documents.Entities;
using DocumentTemplate = ApiWorker.Documents.Entities.Template;

namespace ApiWorker.Documents.Interfaces;

/// <summary>
/// Manages document templates (upload, list, set default).
/// </summary>
public interface ITemplateService
{
    /// <summary>Gets default template for a business and document type</summary>
    Task<DocumentTemplate?> GetDefaultTemplateAsync(Guid businessId, DocumentType type, CancellationToken ct = default);
    
    /// <summary>Lists all templates for a business</summary>
    Task<List<DocumentTemplate>> ListTemplatesAsync(Guid businessId, DocumentType? type = null, CancellationToken ct = default);
    
    /// <summary>Gets a single template by ID</summary>
    Task<DocumentTemplate?> GetTemplateAsync(Guid templateId, CancellationToken ct = default);
    
    /// <summary>Uploads a new template</summary>
    Task<DocumentTemplate> UploadTemplateAsync(Guid businessId, DocumentType type, string name, Stream templateFile, CancellationToken ct = default);
    
    /// <summary>Sets a template as default for a business</summary>
    Task SetDefaultTemplateAsync(Guid templateId, CancellationToken ct = default);
}
